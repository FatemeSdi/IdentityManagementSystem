using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Helpers;
using IdentityManagementSystem.API.Models;
using IdentityManagementSystem.API.Models.ViewModels;
using IdentityManagementSystem.API.Services.Logging;
using IdentityManagementSystem.API.Services.Sms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            string filterStatus = "")
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

            // فقط متقاضی (RoleId == 2) به درخواست‌های خودش محدود می‌شه.
            // کارشناس حراست (RoleId == 1) و ادمین (RoleId == 3) باید همه‌ی درخواست‌ها رو
            // برای بررسی/تایید/رد ببینن — نه فقط چیزی که خودشون ثبت کردن.
            bool isApplicant = currentUser.UserRoles.Any(ur => ur.RoleId == 2);
            if (isApplicant)
            {
                query = query.Where(r =>
                    r.CreatedBy == username ||
                    r.CreatedBy == currentUser.UserId.ToString());
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
                    IsMatch = r.IsMatch ?? false,
                    IsExist = r.IsExist ?? false,
                    IsNationalIdInResponse = r.IsNationalIdInResponse ?? false,
                    IsNationalIdInLawyers = r.IsNationalIdInLawyers ?? false,
                    ValidateByExpert = r.ValidateByExpert,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy
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

                // 8. لاگ
                await _actionLogger.Info(userId, "UpdateValidationStatus",
                    $"{statusName} درخواست #{requestId} توسط کارشناس");

                // 9. اطلاع‌رسانی پیامکی به متقاضی — عدم موفقیت پیامک نباید ثبت وضعیت رو خراب کنه
                if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                {
                    var smsText = validateByExpert
                        ? $"کاربر گرامی، درخواست شما با کد پیگیری {request.RequestCode} تایید شد."
                        : $"کاربر گرامی، درخواست شما با کد پیگیری {request.RequestCode} رد شد.{(string.IsNullOrWhiteSpace(description) ? "" : $" دلیل: {description}")}";

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
}