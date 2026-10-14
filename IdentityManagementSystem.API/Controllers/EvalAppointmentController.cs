using System.Data;
using System.Globalization;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Helpers;
using IdentityManagementSystem.API.Models;
using IdentityManagementSystem.API.Services.Logging;
using IdentityManagementSystem.API.Services.Sms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagementSystem.API.Controllers
{
    /// <summary>
    /// نوبت‌دهی ارزیابی:
    ///   * تعریف نوبت‌های خالی هر محوطه (شرکت) — فقط ادمین
    ///   * نوبت‌های خالی برای یک درخواست — کارشناسی که درخواست رو take کرده
    ///   * ثبت نوبت = تایید درخواست + پیامک زمان نوبت به متقاضی
    /// همه‌ی زمان‌ها تو دیتابیس UTC هستن؛ ورودی/خروجی متنی به وقت ایران.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EvalAppointmentController : ControllerBase
    {
        private const string EvalTypeCode = "EVALUATION_APPOINTMENT";
        private const int MaxSlotsPerGenerate = 2000;

        private readonly IdentityManagementSystemContext _context;
        private readonly ILogger<EvalAppointmentController> _logger;
        private readonly UserActionLogger _actionLogger;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly ISmsService _smsService;

        public EvalAppointmentController(
            IdentityManagementSystemContext context,
            ILogger<EvalAppointmentController> logger,
            UserActionLogger actionLogger,
            EncryptionHelper encryptionHelper,
            ISmsService smsService)
        {
            _context = context;
            _logger = logger;
            _actionLogger = actionLogger;
            _encryptionHelper = encryptionHelper;
            _smsService = smsService;
        }

        // ========================= تعریف نوبت‌ها (ادمین) =========================

        /// <summary>
        /// لیست نوبت‌های یک شرکت در بازه‌ی تاریخ (میلادی «yyyy/M/d» به وقت ایران) همراه با تعداد رزرو.
        /// </summary>
        [HttpGet("Slots")]
        public async Task<IActionResult> GetSlots(int companyId, string? fromDate = null, string? toDate = null)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized(new { success = false, message = "کاربر شناسایی نشد." });
            if (!await IsAdminAsync(userId))
                return StatusCode(403, new { success = false, message = "فقط ادمین مجاز به مدیریت نوبت‌ها است." });

            var (fromUtc, toUtcExclusive) = ResolveRangeUtc(fromDate, toDate, defaultDays: 31);

            var slots = await _context.EvalAppointmentSlots
                .Where(s => s.CompanyId == companyId && s.StartAt >= fromUtc && s.StartAt < toUtcExclusive)
                .OrderBy(s => s.StartAt)
                .Select(s => new
                {
                    s.SlotId,
                    s.StartAt,
                    s.DurationMinutes,
                    s.Capacity,
                    s.IsActive,
                    Booked = _context.RequestEvals.Count(e => e.SlotId == s.SlotId)
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                items = slots.Select(s => new
                {
                    slotId = s.SlotId,
                    startAt = s.StartAt,
                    text = PersianDateText.Format(s.StartAt),
                    durationMinutes = s.DurationMinutes,
                    capacity = s.Capacity,
                    booked = s.Booked,
                    isActive = s.IsActive
                })
            });
        }

        /// <summary>
        /// ساخت گروهی نوبت‌ها: برای هر روزِ بازه که جزو روزهای انتخابی هفته‌ست، از ساعت شروع تا پایان
        /// با طول ثابت. نوبت‌های تکراری (همون شرکت + همون زمان) و نوبت‌های گذشته نادیده گرفته می‌شن.
        /// </summary>
        [HttpPost("Slots/Generate")]
        public async Task<IActionResult> GenerateSlots([FromBody] GenerateSlotsViewModel model)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized(new { success = false, message = "کاربر شناسایی نشد." });
            if (!await IsAdminAsync(userId))
                return StatusCode(403, new { success = false, message = "فقط ادمین مجاز به مدیریت نوبت‌ها است." });

            if (!await _context.Companies.AnyAsync(c => c.CompanyId == model.CompanyId && c.IsActive))
                return BadRequest(new { success = false, message = "شرکت انتخاب‌شده معتبر نیست." });

            if (!TryParseDate(model.FromDate, out var fromLocal) || !TryParseDate(model.ToDate, out var toLocal) || toLocal < fromLocal)
                return BadRequest(new { success = false, message = "بازه‌ی تاریخ نامعتبر است." });

            if (!TimeSpan.TryParseExact(model.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var startTime)
                || !TimeSpan.TryParseExact(model.EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out var endTime)
                || endTime <= startTime)
                return BadRequest(new { success = false, message = "ساعت شروع/پایان نامعتبر است." });

            if (model.DurationMinutes < 5 || model.DurationMinutes > 480)
                return BadRequest(new { success = false, message = "طول هر نوبت باید بین ۵ تا ۴۸۰ دقیقه باشد." });
            if (model.Capacity < 1 || model.Capacity > 100)
                return BadRequest(new { success = false, message = "ظرفیت هر نوبت باید بین ۱ تا ۱۰۰ باشد." });
            if (model.DaysOfWeek == null || model.DaysOfWeek.Count == 0)
                return BadRequest(new { success = false, message = "حداقل یک روز هفته را انتخاب کنید." });

            var days = model.DaysOfWeek.Where(d => d >= 0 && d <= 6).Select(d => (DayOfWeek)d).ToHashSet();
            var nowUtc = DateTime.UtcNow;

            var candidates = new List<DateTime>();
            for (var day = fromLocal.Date; day <= toLocal.Date; day = day.AddDays(1))
            {
                if (!days.Contains(day.DayOfWeek))
                    continue;

                for (var t = startTime; t + TimeSpan.FromMinutes(model.DurationMinutes) <= endTime; t += TimeSpan.FromMinutes(model.DurationMinutes))
                {
                    var startUtc = PersianDateText.FromIranLocal(day + t);
                    if (startUtc > nowUtc)
                        candidates.Add(startUtc);
                }

                if (candidates.Count > MaxSlotsPerGenerate)
                    return BadRequest(new { success = false, message = $"تعداد نوبت‌ها بیش از {MaxSlotsPerGenerate} است؛ بازه را کوچک‌تر کنید." });
            }

            if (candidates.Count == 0)
                return Ok(new { success = true, created = 0, skipped = 0, message = "هیچ نوبت آینده‌ای در این بازه ساخته نشد." });

            var minUtc = candidates.Min();
            var maxUtc = candidates.Max();
            var existing = (await _context.EvalAppointmentSlots
                    .Where(s => s.CompanyId == model.CompanyId && s.StartAt >= minUtc && s.StartAt <= maxUtc)
                    .Select(s => s.StartAt)
                    .ToListAsync())
                .ToHashSet();

            var createdBy = User.Identity?.Name ?? userId.ToString();
            var toAdd = candidates
                .Where(c => !existing.Contains(c))
                .Select(c => new EvalAppointmentSlot
                {
                    CompanyId = model.CompanyId,
                    StartAt = c,
                    DurationMinutes = model.DurationMinutes,
                    Capacity = model.Capacity,
                    IsActive = true,
                    CreatedAt = nowUtc,
                    CreatedBy = createdBy
                })
                .ToList();

            _context.EvalAppointmentSlots.AddRange(toAdd);
            await _context.SaveChangesAsync();

            await _actionLogger.Info(userId, "EvalSlots_Generate",
                $"CompanyId={model.CompanyId}, From={model.FromDate}, To={model.ToDate}, Created={toAdd.Count}, Skipped={candidates.Count - toAdd.Count}");

            return Ok(new
            {
                success = true,
                created = toAdd.Count,
                skipped = candidates.Count - toAdd.Count,
                message = $"{toAdd.Count} نوبت ساخته شد" + (candidates.Count > toAdd.Count ? $" ({candidates.Count - toAdd.Count} نوبت از قبل وجود داشت)." : ".")
            });
        }

        /// <summary>
        /// حذف یک نوبت — فقط اگه هنوز هیچ درخواستی روش رزرو نشده باشه.
        /// </summary>
        [HttpDelete("Slots/{slotId}")]
        public async Task<IActionResult> DeleteSlot(int slotId)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized(new { success = false, message = "کاربر شناسایی نشد." });
            if (!await IsAdminAsync(userId))
                return StatusCode(403, new { success = false, message = "فقط ادمین مجاز به مدیریت نوبت‌ها است." });

            var slot = await _context.EvalAppointmentSlots.FindAsync(slotId);
            if (slot == null)
                return NotFound(new { success = false, message = "نوبت یافت نشد." });

            if (await _context.RequestEvals.AnyAsync(e => e.SlotId == slotId))
                return Conflict(new { success = false, message = "این نوبت برای متقاضی رزرو شده و قابل حذف نیست." });

            _context.EvalAppointmentSlots.Remove(slot);
            await _context.SaveChangesAsync();

            await _actionLogger.Info(userId, "EvalSlots_Delete", $"SlotId={slotId}, CompanyId={slot.CompanyId}, StartAt={slot.StartAt:O}");
            return Ok(new { success = true, message = "نوبت حذف شد." });
        }

        // ========================= نوبت‌دهی (کارشناس) =========================

        /// <summary>
        /// نوبت‌های خالیِ محوطه‌ی شرکتِ این درخواست، از الان تا days روز آینده — گروه‌بندی بر اساس روز.
        /// فقط برای کارشناسی که درخواست رو take کرده (یا ادمین).
        /// </summary>
        [HttpGet("FreeSlots")]
        public async Task<IActionResult> GetFreeSlots(long requestId, int days = 31)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized(new { success = false, message = "کاربر شناسایی نشد." });

            var request = await _context.Request
                .Include(r => r.RequestType)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            var denied = await CheckCanSetAppointmentAsync(request, userId);
            if (denied != null)
                return denied;

            days = Math.Clamp(days, 1, 92);
            var nowUtc = DateTime.UtcNow;
            var untilUtc = nowUtc.AddDays(days);

            var free = await _context.EvalAppointmentSlots
                .Where(s => s.CompanyId == request!.CompanyId && s.IsActive && s.StartAt > nowUtc && s.StartAt < untilUtc)
                .Select(s => new
                {
                    s.SlotId,
                    s.StartAt,
                    s.Capacity,
                    Booked = _context.RequestEvals.Count(e => e.SlotId == s.SlotId)
                })
                .Where(s => s.Booked < s.Capacity)
                .OrderBy(s => s.StartAt)
                .ToListAsync();

            var grouped = free
                .GroupBy(s => PersianDateText.ToIranLocal(s.StartAt).Date)
                .Select(g => new
                {
                    // متن روز بدون ساعت — مثلاً «پنج‌شنبه ۲۳ مهر ۱۴۰۵»
                    dayText = PersianDateText.Format(g.First().StartAt).Split(" ساعت ")[0],
                    slots = g.Select(s => new
                    {
                        slotId = s.SlotId,
                        time = PersianDateText.FormatTime(s.StartAt),
                        remaining = s.Capacity - s.Booked
                    })
                });

            return Ok(new { success = true, days = grouped });
        }

        /// <summary>
        /// ثبت نوبت برای درخواست نوبت‌دهی ارزیابی = تایید درخواست + پیامک زمان نوبت به متقاضی.
        /// قفل روی ردیف نوبت و درخواست (UPDLOCK/HOLDLOCK) تا دو ثبت همزمان ظرفیت رو رد نکنن.
        /// عمداً مجوز CanValidateRequest نمی‌خواد — هر متصدی‌ای که درخواست رو take کرده (و take فقط برای عضو
        /// گروه همون محوطه ممکنه) می‌تونه نوبت ثبت کنه؛ این چک تو CheckCanSetAppointmentAsync انجام می‌شه.
        /// </summary>
        [HttpPost("SetAppointment")]
        public async Task<IActionResult> SetAppointment([FromBody] SetAppointmentViewModel model)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized(new { success = false, message = "کاربر شناسایی نشد." });

            var username = User.Identity?.Name ?? userId.ToString();
            Request? request;
            EvalAppointmentSlot? slot;

            // دو حالت: انتخاب از نوبت‌های تعریف‌شده (SlotId) یا تاریخ/ساعت دلخواه کارشناس (Date + Time به وقت ایران)
            DateTime? customStartUtc = null;
            if (model.SlotId <= 0)
            {
                if (!TryParseDate(model.Date, out var day)
                    || !TimeSpan.TryParseExact(model.Time?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var time))
                    return BadRequest(new { success = false, message = "تاریخ و ساعت نوبت را به‌درستی وارد کنید." });
                customStartUtc = PersianDateText.FromIranLocal(day.Date + time);
            }

            await using (var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM [Define].[Request] WITH (UPDLOCK, HOLDLOCK) WHERE [RequestId] = {model.RequestId}");

                request = await _context.Request
                    .Include(r => r.RequestType)
                    .Include(r => r.Company)
                    .Include(r => r.Eval)
                    .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

                var denied = await CheckCanSetAppointmentAsync(request, userId);
                if (denied != null)
                    return denied;

                if (customStartUtc.HasValue)
                {
                    // قفل روی (شرکت + زمان) — روی ایندکس یکتا، پس دو ثبت همزمانِ همون ساعت سریالی می‌شن
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT 1 FROM [Define].[EvalAppointmentSlot] WITH (UPDLOCK, HOLDLOCK) WHERE [CompanyId] = {request!.CompanyId} AND [StartAt] = {customStartUtc.Value}");

                    slot = await _context.EvalAppointmentSlots
                        .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId && s.StartAt == customStartUtc.Value);

                    if (slot == null)
                    {
                        if (customStartUtc.Value <= DateTime.UtcNow)
                            return BadRequest(new { success = false, message = "زمان انتخاب‌شده گذشته است." });

                        slot = new EvalAppointmentSlot
                        {
                            CompanyId = request.CompanyId!.Value,
                            StartAt = customStartUtc.Value,
                            DurationMinutes = 30,
                            Capacity = 1,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = username
                        };
                        _context.EvalAppointmentSlots.Add(slot);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT 1 FROM [Define].[EvalAppointmentSlot] WITH (UPDLOCK, HOLDLOCK) WHERE [SlotId] = {model.SlotId}");
                    slot = await _context.EvalAppointmentSlots.FirstOrDefaultAsync(s => s.SlotId == model.SlotId);
                }
                if (slot == null || !slot.IsActive || slot.CompanyId != request!.CompanyId)
                    return BadRequest(new { success = false, message = "نوبت انتخاب‌شده برای محوطه‌ی این درخواست معتبر نیست." });
                if (slot.StartAt <= DateTime.UtcNow)
                    return BadRequest(new { success = false, message = "زمان این نوبت گذشته است." });

                var booked = await _context.RequestEvals.CountAsync(e => e.SlotId == slot.SlotId);
                if (booked >= slot.Capacity)
                    return Conflict(new { success = false, message = customStartUtc.HasValue
                        ? "این ساعت در این محوطه قبلاً برای متقاضی دیگری رزرو شده است؛ ساعت دیگری انتخاب کنید."
                        : "ظرفیت این نوبت تکمیل شده است؛ نوبت دیگری انتخاب کنید." });

                var nowUtc = DateTime.UtcNow;
                request.Eval!.SlotId = slot.SlotId;
                request.Eval.AppointmentSetBy = userId;
                request.Eval.AppointmentSetAt = nowUtc;
                request.ValidateByExpert = true;
                request.UpdatedAt = nowUtc;
                request.UpdatedBy = username;

                var appointmentText = PersianDateText.Format(slot.StartAt);
                _context.RequestHistory.Add(new RequestHistory
                {
                    RequestId = request.RequestId,
                    StatusId = 2, // ۲ = تایید شده
                    ExpertId = userId.ToString(),
                    ActionDescription = $"نوبت ارزیابی تعیین شد: {appointmentText} — محوطه شرکت {request.Company?.CompanyName}",
                    CreatedAt = nowUtc,
                    UpdatedStatus = "تأیید شده",
                    UpdatedStatusBy = username,
                    UpdatedStatusDate = nowUtc
                });

                var cartableItem = await _context.CartableItems.FirstOrDefaultAsync(ci => ci.RequestId == request.RequestId);
                if (cartableItem != null)
                {
                    cartableItem.Status = "تأیید شده";
                    cartableItem.ViewedAt ??= nowUtc;
                    cartableItem.UpdatedAt = nowUtc;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            var text = PersianDateText.Format(slot.StartAt);
            await _actionLogger.Info(userId, "EvalAppointment_Set", $"RequestId={request!.RequestId}, SlotId={slot.SlotId}, StartAt={slot.StartAt:O}");

            // پیامک — best-effort، شکستش نباید ثبت نوبت رو برگردونه
            var mobile = _encryptionHelper.DecryptMobile(request);
            bool smsSent = false;
            if (!string.IsNullOrWhiteSpace(mobile))
            {
                var smsText = $"کاربر گرامی، نوبت ارزیابی شما در محوطه شرکت {request.Company?.CompanyName} روز {text} تعیین شد. لطفاً به‌موقع در محل حاضر باشید.\nکد پیگیری: {request.TrackingCode}\nاداره کل بنادر و دریانوردی استان هرمزگان";
                smsSent = await SendAndLogSmsAsync(mobile, smsText, request.RequestId, userId);
            }
            else
            {
                _logger.LogWarning("درخواست {RequestId} شماره موبایل رمزنگاری‌شده ندارد؛ پیامک نوبت ارسال نشد.", request.RequestId);
            }

            return Ok(new
            {
                success = true,
                appointmentText = text,
                smsSent,
                message = smsSent
                    ? $"نوبت {text} ثبت و برای متقاضی پیامک شد."
                    : $"نوبت {text} ثبت شد، اما ارسال پیامک ناموفق بود."
            });
        }

        // ========================= کمکی =========================

        /// <summary>
        /// null یعنی مجازه؛ در غیر این صورت پاسخ خطا. درخواست باید از نوع نوبت‌دهی ارزیابی، هنوز بدون
        /// نتیجه، و take‌شده توسط همین کاربر باشه (ادمین مستثناست).
        /// </summary>
        private async Task<IActionResult?> CheckCanSetAppointmentAsync(Request? request, long userId)
        {
            if (request == null)
                return NotFound(new { success = false, message = "درخواست یافت نشد." });
            if (request.RequestType?.Code != EvalTypeCode)
                return BadRequest(new { success = false, message = "این درخواست از نوع نوبت‌دهی ارزیابی نیست." });
            if (request.ValidateByExpert.HasValue)
                return BadRequest(new { success = false, message = "برای این درخواست قبلاً نتیجه ثبت شده است." });
            if (request.CompanyId == null)
                return BadRequest(new { success = false, message = "محوطه‌ی (شرکت) این درخواست مشخص نیست." });
            if (request.AssignedTo != userId && !await IsAdminAsync(userId))
                return BadRequest(new { success = false, message = "ابتدا باید این درخواست را از کارتابل take کنید." });
            return null;
        }

        private async Task<bool> SendAndLogSmsAsync(string mobile, string text, long requestId, long userId)
        {
            var log = new SmsLog
            {
                UserId = userId,
                RequestId = requestId,
                MobileNumberEnc = _encryptionHelper.Encrypt(mobile),
                MobileNumberHash = _encryptionHelper.ComputeSearchHash(mobile),
                // مقادیر مجاز طبق CK_Sms_Purpose: General/RequestRejected/RequestApproved/RequestStageUpdate/Otp
                Purpose = "RequestApproved",
                MessageText = text,
                Status = "Queued",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var result = await _smsService.SendAsync(mobile, text);
                log.Status = result.IsSuccess ? "Sent" : "Failed";
                log.ErrorMessage = result.IsSuccess ? null : (result.ErrorMessage ?? result.RawResponse);
                log.SentAt = result.IsSuccess ? DateTime.UtcNow : null;
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, "خطا در ارسال پیامک نوبت ارزیابی برای درخواست {RequestId}", requestId);
            }

            try
            {
                _context.SmsLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ثبت لاگ پیامک نوبت ارزیابی برای درخواست {RequestId} ناموفق بود", requestId);
            }

            return log.Status == "Sent";
        }

        private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirst("UserId")?.Value, out userId);

        private Task<bool> IsAdminAsync(long userId) =>
            _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == 3);

        private static bool TryParseDate(string? value, out DateTime date) =>
            DateTime.TryParseExact(value?.Trim(), "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        /// <summary>بازه‌ی روزهای ایران (میلادی «yyyy/M/d») → بازه‌ی UTC؛ پیش‌فرض: امروز تا defaultDays روز بعد.</summary>
        private static (DateTime fromUtc, DateTime toUtcExclusive) ResolveRangeUtc(string? fromDate, string? toDate, int defaultDays)
        {
            var todayIran = PersianDateText.ToIranLocal(DateTime.UtcNow).Date;
            var fromLocal = TryParseDate(fromDate, out var f) ? f.Date : todayIran;
            var toLocal = TryParseDate(toDate, out var t) ? t.Date : fromLocal.AddDays(defaultDays);
            return (PersianDateText.FromIranLocal(fromLocal), PersianDateText.FromIranLocal(toLocal.AddDays(1)));
        }
    }

    public class GenerateSlotsViewModel
    {
        public int CompanyId { get; set; }
        public string FromDate { get; set; } = string.Empty;   // میلادی yyyy/M/d (روز ایران)
        public string ToDate { get; set; } = string.Empty;
        public List<int>? DaysOfWeek { get; set; }             // System.DayOfWeek: 6=شنبه، 0=یک‌شنبه، ...
        public string StartTime { get; set; } = "08:00";       // HH:mm به وقت ایران
        public string EndTime { get; set; } = "14:00";
        public int DurationMinutes { get; set; } = 30;
        public int Capacity { get; set; } = 1;
    }

    public class SetAppointmentViewModel
    {
        public long RequestId { get; set; }

        // یا نوبت تعریف‌شده...
        public int SlotId { get; set; }

        // ...یا تاریخ/ساعت دلخواه کارشناس (وقتی SlotId خالیه): میلادی yyyy/M/d و HH:mm به وقت ایران
        public string? Date { get; set; }
        public string? Time { get; set; }
    }
}
