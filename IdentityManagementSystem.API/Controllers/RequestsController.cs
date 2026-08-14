using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Helpers;
using IdentityManagementSystem.API.Models;
using IdentityManagementSystem.API.Models.ViewModels;
using IdentityManagementSystem.API.Services.Logging;
using IdentityManagementSystem.API.Services.Sms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace IdentityManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RequestController : ControllerBase
    {
        private readonly IdentityManagementSystemContext _context;
        private readonly ILogger<RequestController> _logger;
        private readonly UserActionLogger _actionLogger;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly ISmsService _smsService;

        public RequestController(
            IdentityManagementSystemContext context,
            ILogger<RequestController> logger,
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

        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<RequestViewModel>>> GetAll(
            int page = 1,
            int pageSize = 10,
            string search = "",
            string filterStatus = "",
            string? fromDate = null,
            string? toDate = null)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var currentUser = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (currentUser == null)
                return Unauthorized();

            var query = _context.Request.AsQueryable();

            // متقاضی (RoleId == 2) فقط درخواست‌های خودش رو می‌بینه.
            // ادمین (RoleId == 3) همه‌ی درخواست‌ها رو می‌بینه (نظارت کامل).
            // کارشناس (RoleId == 1) فقط درخواست‌های گروهی که عضوشه رو می‌بینه، و توی اون گروه
            // فقط چیزی که هنوز کسی take نکرده (AssignedTo == null) یا خودش take کرده.
            bool isApplicant = currentUser.UserRoles.Any(ur => ur.RoleId == 2);
            bool isAdmin = currentUser.UserRoles.Any(ur => ur.RoleId == 3);

            if (isApplicant)
            {
                query = query.Where(r =>
                    r.CreatedBy == username ||
                    r.CreatedBy == currentUser.UserId.ToString());
            }
            else if (!isAdmin)
            {
                var myGroupIds = await _context.UserGroups
                    .Where(ug => ug.UserId == currentUser.UserId)
                    .Select(ug => ug.GroupId)
                    .ToListAsync();

                query = query.Where(r =>
                    r.GroupId != null && myGroupIds.Contains(r.GroupId.Value) &&
                    (r.AssignedTo == null || r.AssignedTo == currentUser.UserId));

                // کارتابل کارشناس پیش‌فرض فقط درخواست‌های همون روزه؛ برای دیدن روزهای قبل باید
                // بازه‌ی تاریخ (fromDate/toDate) صریحاً مشخص بشه. تاریخ‌ها به‌صورت میلادی «yyyy/M/d»
                // (تقویم ایران، نه UTC) از سمت کلاینت میان — همون چیزی که پلاگین تاریخ فارسی برمی‌گردونه.
                var (defaultFromUtc, defaultToUtcExclusive) = ResolveDateRangeUtc(fromDate, toDate);
                if (defaultFromUtc.HasValue)
                    query = query.Where(r => r.CreatedAt >= defaultFromUtc.Value);
                if (defaultToUtcExclusive.HasValue)
                    query = query.Where(r => r.CreatedAt < defaultToUtcExclusive.Value);
            }
            else if (!string.IsNullOrWhiteSpace(fromDate) || !string.IsNullOrWhiteSpace(toDate))
            {
                // ادمین/متقاضی پیش‌فرضشون همون رفتار قبلیه (تاریخچه‌ی کامل)؛ فقط اگه صریحاً بازه بدن اعمال می‌شه.
                var (fromUtc, toUtcExclusive) = ResolveDateRangeUtc(fromDate, toDate, defaultToToday: false);
                if (fromUtc.HasValue)
                    query = query.Where(r => r.CreatedAt >= fromUtc.Value);
                if (toUtcExclusive.HasValue)
                    query = query.Where(r => r.CreatedAt < toUtcExclusive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    (r.NationalId != null && r.NationalId.Contains(search)) ||
                    (r.MobileNumber != null && r.MobileNumber.Contains(search)) ||
                    (r.DocumentNumber != null && r.DocumentNumber.Contains(search)) ||
                    (r.VerificationCode != null && r.VerificationCode.Contains(search)) ||
                    (r.RequestCode != null && r.RequestCode.Contains(search)) ||
                    r.RequestId.ToString().Contains(search));
            }

            if (!string.IsNullOrEmpty(filterStatus))
            {
                switch (filterStatus.ToLower())
                {
                    case "approved":
                        query = query.Where(r => r.ValidateByExpert == true);
                        break;

                    case "rejected":
                        query = query.Where(r => r.ValidateByExpert == false);
                        break;

                    case "pending":
                        query = query.Where(r => r.ValidateByExpert == null);
                        break;
                }
            }

            query = query.OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Select(r => new RequestViewModel
                {
                    RequestId = r.RequestId,
                    RequestCode = r.RequestCode,
                    NationalId = r.NationalId,
                    MobileNumber = r.MobileNumber,
                    DocumentNumber = r.DocumentNumber,
                    // اگر encrypted ذخیره شده باشد decrypt، در غیر این صورت همان مقدار
                    VerificationCode = r.VerificationCode,
                    WarehouseReceiptNumber = r.WarehouseReceiptNumber,
                    TrackingCode = r.TrackingCode,
                    IsMatch = r.IsMatch ?? false,
                    IsExist = r.IsExist ?? false,
                    IsNationalIdInResponse = r.IsNationalIdInResponse ?? false,
                    IsNationalIdInLawyers = r.IsNationalIdInLawyers ?? false,
                    ValidateByExpert = r.ValidateByExpert,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy,
                    GroupId = r.GroupId,
                    GroupTitle = r.Group != null ? r.Group.Title : null,
                    AssignedTo = r.AssignedTo,
                    AssignedToName = r.AssignedToUser != null ? (r.AssignedToUser.Name + " " + r.AssignedToUser.LastName) : null,
                    AssignedAt = r.AssignedAt
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PaginatedResponse<RequestViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// پیگیری وضعیت یک درخواست با کد پیگیری + شماره موبایل — هر دو باید با هم مطابقت داشته باشن
        /// (چون کد پیگیری از RequestId مشتق و قابل حدس زدنه، تنهایی برای شناسایی کافی نیست).
        /// </summary>
        [HttpGet("Track")]
        public async Task<IActionResult> Track(string trackingCode, string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingCode) || string.IsNullOrWhiteSpace(mobileNumber))
            {
                return BadRequest(new { success = false, message = "کد پیگیری و شماره موبایل الزامی است." });
            }

            var request = await _context.Request
                .Include(r => r.AssignedToUser)
                .FirstOrDefaultAsync(r => r.TrackingCode == trackingCode.Trim() && r.MobileNumber == mobileNumber.Trim());

            if (request == null)
            {
                return NotFound(new { success = false, message = "درخواستی با این مشخصات یافت نشد." });
            }

            string statusText = request.ValidateByExpert == true
                ? "تایید شده"
                : request.ValidateByExpert == false
                    ? "رد شده"
                    : "در انتظار بررسی";

            // اطلاعات تماس کارشناس فقط برای درخواست‌های نهایی‌شده (تایید/رد) نشون داده می‌شه —
            // نه برای Pending، چون هنوز تصمیمی گرفته نشده که بخوان درباره‌ش تماس بگیرن.
            // عمداً هیچ‌جا وارد متن پیامک نمی‌شه، فقط تو همین پاسخ وب.
            string? handlerName = null;
            string? handlerExtension = null;
            if (request.ValidateByExpert.HasValue && request.AssignedToUser != null)
            {
                handlerName = $"{request.AssignedToUser.Name} {request.AssignedToUser.LastName}".Trim();
                handlerExtension = request.AssignedToUser.Extension;
            }

            return Ok(new
            {
                success = true,
                trackingCode = request.TrackingCode,
                createdAt = request.CreatedAt,
                status = statusText,
                description = request.ValidateByExpert == false ? request.Description : null,
                handlerName,
                handlerExtension
            });
        }

        /// <summary>
        /// لیست درخواست‌های «در انتظار بررسی» یک متقاضی بر اساس شماره موبایل — بدون نیاز به کد پیگیری.
        /// سمت UI قبل از صدا زدن این endpoint باید OTP شماره موبایل رو تایید کرده باشه (کنترل سمت UI/Session).
        /// درخواست‌های نهایی‌شده (رد/تایید) عمداً اینجا برنمی‌گردن — برای اونا کد پیگیری لازمه، نه فقط موبایل.
        /// </summary>
        [HttpGet("TrackByMobile")]
        public async Task<IActionResult> TrackByMobile(string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
            {
                return BadRequest(new { success = false, message = "شماره موبایل الزامی است." });
            }

            var pendingRequests = await _context.Request
                // TrackingCode خالی یعنی رکورد قدیمی/ناقصه (قبل از این‌که تولید خودکار کد پیگیری همیشگی بشه) —
                // بدون کد پیگیری، متقاضی هیچ راهی برای تشخیص این درخواست نداره، پس نشونش نمی‌دیم.
                .Where(r => r.MobileNumber == mobileNumber.Trim() && r.ValidateByExpert == null && r.TrackingCode != null)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    trackingCode = r.TrackingCode,
                    warehouseReceiptNumber = r.WarehouseReceiptNumber,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, items = pendingRequests });
        }

        /// <summary>
        /// تاریخچه‌ی کامل یک درخواست (Sec.RequestHistory) — دقیقاً کی چیکار کرد: ایجاد، take،
        /// تایید/رد (دستی یا سیستمی). IsSystemAction وقتی true‌ه که ExpertId == "System" باشه
        /// (فقط رد خودکار تو AutoRejectAsync)؛ بقیه‌ی ردیف‌ها UpdatedStatusBy رو به اسم واقعی کاربر دارن.
        /// </summary>
        [HttpGet("{id}/History")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<RequestHistoryViewModel>>> GetRequestHistory(long id)
        {
            var exists = await _context.Request.AnyAsync(r => r.RequestId == id);
            if (!exists)
                return NotFound(new { success = false, message = "درخواست یافت نشد." });

            var history = await _context.RequestHistory
                .Include(h => h.Status)
                .Where(h => h.RequestId == id)
                .OrderBy(h => h.CreatedAt)
                .Select(h => new RequestHistoryViewModel
                {
                    LogId = h.LogId,
                    StatusName = h.Status != null ? h.Status.StatusName : null,
                    ActionDescription = h.ActionDescription,
                    UpdatedStatus = h.UpdatedStatus,
                    UpdatedStatusBy = h.UpdatedStatusBy,
                    CreatedAt = h.CreatedAt,
                    IsSystemAction = h.ExpertId == "System"
                })
                .ToListAsync();

            return Ok(history);
        }

        /// <summary>
        /// کارشناس درخواست رو از صف مشترک گروهش «take» می‌کنه. با یه UPDATE شرطی (AssignedTo IS NULL)
        /// انجام می‌شه تا اگه دو کارشناس همزمان take بزنن، فقط اولی موفق بشه (race-safe).
        /// </summary>
        [HttpPost("Take")]
        [Authorize]
        public async Task<IActionResult> Take([FromBody] TakeRequestViewModel model)
        {
            if (!long.TryParse(User.FindFirst("UserId")?.Value, out long userId))
                return Unauthorized(new { success = false, message = "کاربر شناسایی نشد." });

            var request = await _context.Request.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

            if (request == null)
                return NotFound(new { success = false, message = "درخواست یافت نشد." });

            if (request.ValidateByExpert == false)
            {
                await _actionLogger.Warning(userId, "Take_Request", $"RequestId={model.RequestId}, Denied=AlreadyRejected");
                return BadRequest(new { success = false, message = "این درخواست به‌صورت سیستمی رد شده؛ دیگه قابل take نیست." });
            }

            var isMember = request.GroupId != null && await _context.UserGroups
                .AnyAsync(ug => ug.UserId == userId && ug.GroupId == request.GroupId.Value);

            if (!isMember)
            {
                await _actionLogger.Warning(userId, "Take_Request", $"RequestId={model.RequestId}, Denied=NotGroupMember");
                return StatusCode(403, new { success = false, message = "شما عضو گروهی که این درخواست بهش تعلق داره نیستید." });
            }

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Define].[Request] SET [AssignedTo] = {userId}, [AssignedAt] = {DateTime.UtcNow} WHERE [RequestId] = {model.RequestId} AND [AssignedTo] IS NULL");

            if (affected == 0)
            {
                await _actionLogger.Warning(userId, "Take_Request", $"RequestId={model.RequestId}, Denied=AlreadyTaken");
                return Conflict(new { success = false, message = "این درخواست قبلاً توسط کارشناس دیگری take شده است." });
            }

            await _actionLogger.Info(userId, "Take_Request", $"RequestId={model.RequestId}");

            // این take رو تو CartableItem هم ثبت می‌کنیم. CartableId و AssignedTo (که GroupId‌ه) از همون
            // لحظه‌ی ثبت درخواست پر شدن و دست‌نخورده می‌مونن — take فقط IsTakenBy/Status/UpdatedAt رو عوض می‌کنه.
            // عمداً best-effort: خودِ take (که روی Request انجام شد و بالا برگشت داده شد) با شکست این بخش نباید لغو بشه.
            try
            {
                // یه ردیف CartableItem از همون لحظه‌ی ثبت درخواست وجود داره (نگاه کن به ServiceController) —
                // اینجا همون ردیف رو آپدیت می‌کنیم، نه ردیف جدید. اگه به هر دلیلی وجود نداشت (داده‌ی قدیمی‌تر
                // از این تغییر)، همینجا می‌سازیمش (CartableId از روی نوع درخواست، AssignedTo از روی GroupId).
                var cartableItem = await _context.CartableItems.FirstOrDefaultAsync(ci => ci.RequestId == model.RequestId);
                if (cartableItem == null)
                {
                    var requestType = request.RequestTypeId.HasValue
                        ? await _context.RequestTypes.FindAsync(request.RequestTypeId.Value)
                        : null;

                    cartableItem = new CartableItem
                    {
                        RequestId = model.RequestId,
                        CartableId = requestType?.CartableId,
                        AssignedTo = request.GroupId
                    };
                    _context.CartableItems.Add(cartableItem);
                }

                cartableItem.IsTakenBy = userId;
                cartableItem.UpdatedAt = DateTime.UtcNow;
                cartableItem.Status = "Assigned";
                await _context.SaveChangesAsync();

                // این take رو تو RequestHistory هم ثبت می‌کنیم — تا مشخص باشه دقیقاً چه کسی و کِی take کرده
                _context.RequestHistory.Add(new RequestHistory
                {
                    RequestId = model.RequestId,
                    StatusId = 1,
                    ExpertId = userId.ToString(),
                    ActionDescription = "درخواست توسط کارشناس take شد.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedStatus = "Take شده",
                    UpdatedStatusBy = User.Identity?.Name ?? userId.ToString(),
                    UpdatedStatusDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ثبت CartableItem/RequestHistory برای درخواست {RequestId} ناموفق بود (خودِ take انجام شد و معتبره)", model.RequestId);
            }

            return Ok(new { success = true, message = "درخواست با موفقیت take شد." });
        }

        private static TimeZoneInfo GetIranTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
                catch { return TimeZoneInfo.Utc; }
            }
        }

        /// <summary>
        /// بازه‌ی fromDate/toDate (رشته‌ی میلادی «yyyy/M/d» بر مبنای روز تقویمی ایران — همون چیزی که
        /// پلاگین تاریخ فارسی سمت کلاینت تولید می‌کنه) رو به بازه‌ی UTC برای فیلتر CreatedAt تبدیل می‌کنه.
        /// toUtcExclusive مرز بالاییِ exclusive هست، یعنی کل روزِ toDate رو شامل می‌شه.
        /// اگه هیچ‌کدوم داده نشده باشن و defaultToToday=true باشه، بازه‌ی «امروز» (به وقت ایران) برمی‌گرده.
        /// </summary>
        private static (DateTime? fromUtc, DateTime? toUtcExclusive) ResolveDateRangeUtc(string? fromDate, string? toDate, bool defaultToToday = true)
        {
            var iranTz = GetIranTimeZone();

            if (string.IsNullOrWhiteSpace(fromDate) && string.IsNullOrWhiteSpace(toDate))
            {
                if (!defaultToToday)
                    return (null, null);

                var nowIran = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, iranTz);
                var todayStart = DateTime.SpecifyKind(nowIran.Date, DateTimeKind.Unspecified);
                var fromTodayUtc = TimeZoneInfo.ConvertTimeToUtc(todayStart, iranTz);
                return (fromTodayUtc, fromTodayUtc.AddDays(1));
            }

            DateTime? fromUtc = null;
            DateTime? toUtcExclusive = null;

            if (!string.IsNullOrWhiteSpace(fromDate) &&
                DateTime.TryParseExact(fromDate.Trim(), "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromLocal))
            {
                var start = DateTime.SpecifyKind(fromLocal.Date, DateTimeKind.Unspecified);
                fromUtc = TimeZoneInfo.ConvertTimeToUtc(start, iranTz);
            }

            if (!string.IsNullOrWhiteSpace(toDate) &&
                DateTime.TryParseExact(toDate.Trim(), "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toLocal))
            {
                var start = DateTime.SpecifyKind(toLocal.Date, DateTimeKind.Unspecified);
                var toStartUtc = TimeZoneInfo.ConvertTimeToUtc(start, iranTz);
                toUtcExclusive = toStartUtc.AddDays(1);
            }

            return (fromUtc, toUtcExclusive);
        }

        private string? TryDecrypt(string value)
        {
            try
            {
                return _encryptionHelper.Decrypt(value);
            }
            catch
            {
                // اگر plain text باشد همان را برگردان
                return value;
            }
        }

        [HttpPost("CreateNewRequest")]
        [Authorize]
        public async Task<IActionResult> CreateNewRequest(NewRequestViewModel model)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                return Unauthorized("کاربر شناسایی نشد.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newRequest = new Request
                {
                    NationalId = model.NationalId,
                    MobileNumber = model.MobileNumber,
                    DocumentNumber = model.DocumentNumber,
                    VerificationCode = _encryptionHelper.Encrypt(model.VerificationCode), // ذخیره encrypted
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "Unknown",
                    ValidateByExpert = null,
                    RequestCode = GenerateTempRequestCode() // اگر لازم باشد
                };

                _context.Request.Add(newRequest);
                await _context.SaveChangesAsync();

                var shahkarLog = new ShahkarLog
                {
                    NationalId = model.NationalId,
                    MobileNumber = model.MobileNumber,
                    RequestCode = newRequest.RequestCode,
                    ExpertId = userId,
                    RequestId = newRequest.RequestId,
                    CreatedAt = DateTime.UtcNow,
                    IsMatch = false
                };
                _context.ShahkarLog.Add(shahkarLog);

                var verifyLog = new VerifyDocLog
                {
                    DocumentNumber = model.DocumentNumber,
                    VerificationCode = model.VerificationCode.Length > 10 ? model.VerificationCode.Substring(0, 10) : model.VerificationCode, // DB limit 10
                    ResponseText = "",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId.ToString(),
                    RequestId = newRequest.RequestId
                };
                _context.VerifyDocLog.Add(verifyLog);

                var initialHistory = new RequestHistory
                {
                    RequestId = newRequest.RequestId,
                    StatusId = 1,
                    ExpertId = userId.ToString(), // string مطابق اسکیما
                    ActionDescription = "درخواست جدید ایجاد شد و در انتظار بررسی قرار گرفت.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedStatus = "در انتظار بررسی",
                    UpdatedStatusBy = "سیستم",
                    UpdatedStatusDate = DateTime.UtcNow
                };
                _context.RequestHistory.Add(initialHistory);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _actionLogger.Info(userId, "Create_Request", $"RequestId={newRequest.RequestId}, Document={model.DocumentNumber}");

                return Ok(new
                {
                    RequestId = newRequest.RequestId,
                    Message = "درخواست جدید با موفقیت ایجاد شد."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in CreateNewRequest");
                await _actionLogger.Error(userId, "Create_Request", $"Exception: {ex.Message}");
                return StatusCode(500, new { success = false, message = "خطا در ایجاد درخواست." });
            }
        }

        private string GenerateTempRequestCode()
        {
            return "TMP" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        }

        [HttpPost("ValidateRequest")]
        [Authorize(Policy = "CanValidateRequest")]
        public async Task<IActionResult> ValidateRequest([FromBody] ValidateRequestViewModel model)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                return Unauthorized("کاربر شناسایی نشد.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.Request.FindAsync(model.RequestId);
                if (request == null)
                    return NotFound("درخواست یافت نشد.");

                int newStatusId = model.ValidateByExpert ? 2 : 3;
                string statusName = model.ValidateByExpert ? "تأیید شده" : "رد شده";

                if (model.ValidateByExpert == true)
                {
                    var isRead = await _context.VerifyDocLog.AnyAsync(d => d.RequestId == model.RequestId && d.IsRead == true);
                    if (!isRead)
                        return BadRequest("لطفاً ابتدا متن سند را مشاهده و تأیید کنید.");
                }

                if (model.ValidateByExpert == false && string.IsNullOrWhiteSpace(model.Description))
                    return BadRequest("برای رد درخواست، توضیح الزامی است.");

                request.ValidateByExpert = model.ValidateByExpert;
                request.Description = model.Description;
                request.UpdatedAt = DateTime.UtcNow;
                request.UpdatedBy = User.Identity?.Name ?? userId.ToString();
                _context.Request.Update(request);

                var history = new RequestHistory
                {
                    RequestId = request.RequestId,
                    ExpertId = userId.ToString(),
                    StatusId = newStatusId,
                    ActionDescription = model.ValidateByExpert
                        ? $"درخواست تأیید شد. توضیحات: {model.Description}"
                        : $"درخواست رد شد. توضیحات: {model.Description}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedStatus = statusName,
                    UpdatedStatusBy = User.Identity?.Name ?? userId.ToString(),
                    UpdatedStatusDate = DateTime.UtcNow
                };
                _context.RequestHistory.Add(history);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _actionLogger.Info(userId, "Validate_Request", $"{statusName} by expert");

                return Ok(new
                {
                    RequestId = request.RequestId,
                    Message = $"درخواست با موفقیت {statusName}."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in ValidateRequest");
                await _actionLogger.Error(userId, "Validate_Request", $"Exception: {ex.Message}");
                return StatusCode(500, new { success = false, message = "خطا در سرور." });
            }
        }

        [HttpPost("UpdateValidationStatus")]
        [Authorize(Policy = "CanValidateRequest")]
        public async Task<IActionResult> UpdateValidationStatus([FromBody] UpdateValidationStatusViewModel model)
        {
            var requestId = model.RequestId;
            var validateByExpert = model.ValidateByExpert;
            var description = model.Description?.Trim();

            // رد کردن دیگه یه اکشن دستی نیست — فقط سیستم (تو ProcessCombinedRequest) با شکست
            // هر گام از زنجیره‌ی سرویس‌ها می‌تونه درخواست رو رد کنه. متصدی فقط می‌تونه تایید بزنه.
            if (validateByExpert == false)
            {
                return BadRequest(new { success = false, message = "رد درخواست فقط به‌صورت سیستمی و خودکار انجام می‌شود؛ اکشن دستی رد وجود ندارد." });
            }

            try
            {
                // 1. شناسایی کاربر
                if (!long.TryParse(User.FindFirst("UserId")?.Value, out long userId))
                    return Unauthorized(new { success = false, message = "کاربر شناسایی نشد." });

                var username = User.FindFirst("Username")?.Value
                    ?? User.FindFirst("username")?.Value
                    ?? User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? $"کاربر {userId}";

                // 2. پیدا کردن درخواست
                var request = await _context.Request
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                    return NotFound(new { success = false, message = "درخواست یافت نشد." });

                // 2b. فقط کارشناسی که خودش این درخواست رو take کرده می‌تونه تاییدش کنه (ادمین مستثناست)
                bool isAdminCaller = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == 3);
                if (!isAdminCaller && request.AssignedTo != userId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ ابتدا باید این درخواست را از کارتابل take کنید تا بتوانید آن را تأیید کنید."
                    });
                }

                // 3. شرط تأیید: باید سند خوانده شده باشد
                if (validateByExpert == true)
                {
                    var latestLog = await _context.VerifyDocLog
                        .Where(v => v.RequestId == requestId)
                        .OrderByDescending(v => v.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (latestLog == null)
                        return BadRequest(new
                        {
                            success = false,
                            message = "❌ سندی برای این درخواست آپلود نشده. ابتدا سند را بررسی کنید."
                        });

                    if (!(latestLog.IsRead ?? false))
                        return BadRequest(new
                        {
                            success = false,
                            message = "❌ شما هنوز تیک «سند مشاهده شد» را نزده‌اید. لطفاً ابتدا سند را بخوانید و تأیید کنید."
                        });

                    // رد کاملاً سیستمیه؛ پس تایید دستی فقط وقتی مجازه که همه‌ی گام‌های احراز از قبل true بوده باشن.
                    // (این چک عمداً مستقل از فرانت‌اِنده — حتی اگه کسی مستقیم این endpoint رو صدا بزنه هم رعایت می‌شه.)
                    bool allChecksPassed = request.IsMatch == true
                        && request.IsExist == true
                        && request.IsNationalIdInResponse == true
                        && request.IsNationalIdInLawyers == true;

                    if (!allChecksPassed)
                        return BadRequest(new
                        {
                            success = false,
                            message = "❌ این درخواست هنوز همه‌ی مراحل احراز را با موفقیت پشت سر نگذاشته؛ تأیید دستی امکان‌پذیر نیست."
                        });
                }

                // 4. شرط رد: توضیح الزامی است
                if (validateByExpert == false && string.IsNullOrWhiteSpace(description))
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ برای رد درخواست، وارد کردن دلیل الزامی است."
                    });

                // 5. تعیین وضعیت
                int statusId = validateByExpert ? 2 : 3;
                string statusName = validateByExpert ? "تأیید شده" : "رد شده";

                // 6. به‌روزرسانی درخواست
                request.ValidateByExpert = validateByExpert;
                request.Description = string.IsNullOrWhiteSpace(description) ? null : description;
                request.UpdatedAt = DateTime.UtcNow;
                request.UpdatedBy = username;

                // 7. ثبت تاریخچه
                var history = new RequestHistory
                {
                    RequestId = requestId,
                    StatusId = statusId,
                    ExpertId = userId.ToString(),
                    ActionDescription = validateByExpert
                        ? "درخواست توسط کارشناس تأیید شد"
                        : $"درخواست رد شد: {description}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedStatus = statusName,
                    UpdatedStatusBy = username,
                    UpdatedStatusDate = DateTime.UtcNow
                };

                _context.Request.Update(request);
                _context.RequestHistory.Add(history);
                await _context.SaveChangesAsync();

                // 7b. وضعیت CartableItem مرتبط رو هم هماهنگ می‌کنیم (best-effort، شکستش نباید تایید/رد رو لغو کنه)
                try
                {
                    var cartableItem = await _context.CartableItems.FirstOrDefaultAsync(ci => ci.RequestId == requestId);
                    if (cartableItem != null)
                    {
                        cartableItem.Status = statusName;
                        cartableItem.ViewedAt = cartableItem.ViewedAt ?? DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception cartableEx)
                {
                    _logger.LogWarning(cartableEx, "به‌روزرسانی CartableItem برای درخواست {RequestId} ناموفق بود", requestId);
                }

                // 8. لاگ
                await _actionLogger.Info(userId, "UpdateValidationStatus",
                    $"{statusName} درخواست #{requestId} توسط کارشناس");

                // 9. اطلاع‌رسانی پیامکی به متقاضی — عدم موفقیت پیامک نباید ثبت وضعیت رو خراب کنه
                if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                {
                    var smsText = validateByExpert
                        ? $"کاربر گرامی، درخواست شما با کد پیگیری {request.TrackingCode ?? request.RequestCode} تایید شد."
                        : $"کاربر گرامی، درخواست شما با کد پیگیری {request.TrackingCode ?? request.RequestCode} رد شد.{(string.IsNullOrWhiteSpace(description) ? "" : $" دلیل: {description}")}";

                    var smsLog = new SmsLog
                    {
                        UserId = userId,
                        RequestId = requestId,
                        MobileNumberEnc = _encryptionHelper.Encrypt(request.MobileNumber),
                        MobileNumberHash = _encryptionHelper.ComputeSearchHash(request.MobileNumber),
                        // مقادیر مجاز طبق CK_Sms_Purpose: General/RequestRejected/RequestApproved/RequestStageUpdate/Otp
                        Purpose = validateByExpert ? "RequestApproved" : "RequestRejected",
                        MessageText = smsText,
                        CreatedAt = DateTime.UtcNow
                    };

                    try
                    {
                        var smsResult = await _smsService.SendAsync(request.MobileNumber, smsText);
                        smsLog.Status = smsResult.IsSuccess ? "Sent" : "Failed";
                        smsLog.ErrorMessage = smsResult.IsSuccess ? null : (smsResult.ErrorMessage ?? smsResult.RawResponse);
                        smsLog.SentAt = smsResult.IsSuccess ? DateTime.UtcNow : null;

                        if (!smsResult.IsSuccess)
                        {
                            _logger.LogWarning("پیامک اطلاع‌رسانی وضعیت برای درخواست {RequestId} ارسال نشد: {Error}",
                                requestId, smsLog.ErrorMessage);
                        }
                    }
                    catch (Exception smsEx)
                    {
                        smsLog.Status = "Failed";
                        smsLog.ErrorMessage = smsEx.Message;
                        _logger.LogError(smsEx, "خطا در ارسال پیامک اطلاع‌رسانی برای درخواست {RequestId}", requestId);
                    }

                    try
                    {
                        _context.SmsLogs.Add(smsLog);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "ثبت لاگ پیامک برای درخواست {RequestId} در دیتابیس ناموفق بود", requestId);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"درخواست با موفقیت {statusName}.",
                    data = new { requestId, statusName }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در UpdateValidationStatus - RequestId: {RequestId}", requestId);
                await _actionLogger.Error(User, "UpdateValidationStatus", ex.Message);
                return StatusCode(500, new { success = false, message = "⚠️ خطای سرور رخ داد." });
            }
        }
    }

    public class NewRequestViewModel
    {
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class UpdateValidationStatusViewModel
    {
        public long RequestId { get; set; }
        public bool ValidateByExpert { get; set; }
        public string? Description { get; set; }
    }

    public class TakeRequestViewModel
    {
        public long RequestId { get; set; }
    }
}