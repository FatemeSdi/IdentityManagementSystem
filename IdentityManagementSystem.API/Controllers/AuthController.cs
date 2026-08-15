using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityManagementSystem.API.Models.ViewModels;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Models;
using IdentityManagementSystem.API.Services;
using IdentityManagementSystem.API.Services.Sms;
using IdentityManagementSystem.API.Helpers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IdentityManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IdentityManagementSystemContext _context;
        private readonly TokenService _tokenService;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly ISmsService _smsService;

        public AuthController(
            IdentityManagementSystemContext context,
            TokenService tokenService,
            EncryptionHelper encryptionHelper,
            ISmsService smsService)
        {
            _context = context;
            _tokenService = tokenService;
            _encryptionHelper = encryptionHelper;
            _smsService = smsService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginViewModel loginViewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == loginViewModel.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginViewModel.Password, user.PasswordHash))
            {
                await LogAction(user?.UserId ?? 0, "Login_Failed", loginViewModel.Username, "نام کاربری یا رمز عبور اشتباه است");
                return Unauthorized("نام کاربری یا رمز عبور اشتباه است.");
            }

            // Set convenience properties from first role (if any)
            var firstRole = user.UserRoles?.FirstOrDefault()?.Role;
            user.Role = firstRole;
            user.RoleId = firstRole?.RoleId ?? 0;

            // بروزرسانی آخرین ورود
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // تولید توکن‌ها
            var tokens = await _tokenService.GenerateTokensAsync(user);

            await LogAction(user.UserId, "Login_Success", user.Username, "ورود موفق");

            // خروجی شامل اطلاعات کاربر و توکن‌ها
            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Name,
                user.LastName,
                Role = firstRole != null ? new
                {
                    firstRole.RoleId,
                    firstRole.RoleName
                } : null,
                Tokens = tokens
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestViewModel model)
        {
            var refreshToken = await _tokenService.GetRefreshTokenAsync(model.RefreshToken);
            if (refreshToken == null)
                return Unauthorized("Refresh Token نامعتبر یا منقضی شده است.");

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == refreshToken.UserId);
            if (user == null)
                return Unauthorized("کاربر یافت نشد.");

            // Set convenience
            var firstRole = user.UserRoles?.FirstOrDefault()?.Role;
            user.Role = firstRole;
            user.RoleId = firstRole?.RoleId ?? 0;

            await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);
            var newTokens = await _tokenService.GenerateTokensAsync(user);

            await LogAction(user.UserId, "Refresh_Success", user.Username, "Token refreshed");

            return Ok(new
            {
                Message = "توکن با موفقیت تمدید شد",
                Tokens = newTokens
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserViewModel>> Register(CreateUserViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // بررسی تکراری بودن نام کاربری و کد ملی
            if (await _context.Users.AnyAsync(u => u.Username == viewModel.Username))
                return BadRequest("نام کاربری قبلاً ثبت شده است.");

            if (await _context.Users.AnyAsync(u => u.NationalId == viewModel.NationalId))
                return BadRequest("کد ملی قبلاً ثبت شده است.");

            // ساخت کاربر جدید
            var user = new User
            {
                NationalId = viewModel.NationalId,
                Username = viewModel.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(viewModel.Password),
                Name = viewModel.Name,
                LastName = viewModel.LastName,
                MobileNumber = viewModel.MobileNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // کاربر ثبت می‌شود

            // ثبت نقش از طریق UserRoles (مطابق اسکیمای DB)
            if (viewModel.RoleId > 0)
            {
                var userRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = viewModel.RoleId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            // بارگذاری نقش
            user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (user == null)
                return StatusCode(500, "خطا در ثبت کاربر.");

            var roleName = user.UserRoles?.FirstOrDefault()?.Role?.RoleName ?? "بدون نقش";

            // ثبت لاگ
            await LogAction(user.UserId, "Register_Success", user.Username, "User registered");

            // آماده‌سازی خروجی
            var result = new UserViewModel
            {
                UserId = user.UserId,
                NationalId = user.NationalId,
                Username = user.Username,
                Name = user.Name,
                LastName = user.LastName,
                Role = roleName,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                IsActive = user.IsActive,
                MobileNumber = user.MobileNumber
            };

            return CreatedAtAction(nameof(GetUsers), new { id = user.UserId }, result);
        }

        #region ForgotPassword

        private static string GenerateOtpCode() => RandomNumberGenerator.GetInt32(10000, 100000).ToString();

        [HttpPost("forgot-password/start")]
        [AllowAnonymous]
        public async Task<IActionResult> StartForgotPassword([FromBody] ForgotPasswordIdentityRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.NationalId) || string.IsNullOrWhiteSpace(model.MobileNumber))
                return Ok(new { success = false, message = "کدملی و شماره همراه الزامی است." });

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.NationalId == model.NationalId && u.MobileNumber == model.MobileNumber);

            if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.MobileNumber))
            {
                // برای جلوگیری از افشای وجود/عدم وجود کاربر، پیام عمومی برگردانده می‌شود
                return Ok(new { success = false, message = "اطلاعات وارد شده صحیح نیست." });
            }

            var previousOtps = await _context.LoginOtps
                .Where(o => o.UserId == user.UserId && o.Purpose == "PasswordReset" && !o.IsUsed)
                .ToListAsync();
            foreach (var old in previousOtps) old.IsUsed = true;

            var code = GenerateOtpCode();
            var otp = new LoginOtp
            {
                UserId = user.UserId,
                OtpCodeHash = _encryptionHelper.ComputeSearchHash(code),
                Purpose = "PasswordReset",
                RequestedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                MaxAttempts = 5,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.LoginOtps.Add(otp);
            await _context.SaveChangesAsync();

            var smsText = $"کد بازیابی رمز عبور شما: {code}\nاعتبار: ۵ دقیقه";
            var smsLog = new SmsLog
            {
                UserId = user.UserId,
                OtpId = otp.OtpId,
                MobileNumberEnc = _encryptionHelper.Encrypt(user.MobileNumber),
                MobileNumberHash = _encryptionHelper.ComputeSearchHash(user.MobileNumber),
                Purpose = "Otp",
                MessageTemplate = "PasswordResetOtp",
                MessageText = "کد بازیابی رمز عبور برای کاربر ارسال شد.", // masked، بدون کد واقعی
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var smsResult = await _smsService.SendAsync(user.MobileNumber!, smsText);
                smsLog.Status = smsResult.IsSuccess ? "Sent" : "Failed";
                smsLog.ErrorMessage = smsResult.IsSuccess ? null : (smsResult.ErrorMessage ?? smsResult.RawResponse);
                smsLog.SentAt = smsResult.IsSuccess ? DateTime.UtcNow : null;

                _context.SmsLogs.Add(smsLog);
                await _context.SaveChangesAsync();

                if (!smsResult.IsSuccess)
                {
                    return Ok(new { success = false, message = "ارسال پیامک ناموفق بود. لطفاً بعداً تلاش کنید." });
                }
            }
            catch (Exception ex)
            {
                smsLog.Status = "Failed";
                smsLog.ErrorMessage = ex.Message;
                _context.SmsLogs.Add(smsLog);
                await _context.SaveChangesAsync();
                return Ok(new { success = false, message = "ارسال پیامک ناموفق بود. لطفاً بعداً تلاش کنید." });
            }

            await LogAction(user.UserId, "ForgotPassword_CodeSent", user.Username, "کد بازیابی رمز عبور ارسال شد");

            return Ok(new { success = true, message = "کد تأیید برای شماره همراه شما ارسال شد.", expiresInSeconds = 300 });
        }

        [HttpPost("forgot-password/verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyForgotPasswordCode([FromBody] ForgotPasswordVerifyRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.NationalId) || string.IsNullOrWhiteSpace(model.MobileNumber) || string.IsNullOrWhiteSpace(model.Code))
                return Ok(new { success = false, message = "اطلاعات ارسالی ناقص است." });

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.NationalId == model.NationalId && u.MobileNumber == model.MobileNumber);
            if (user == null)
                return Ok(new { success = false, message = "کد تأیید نامعتبر است." });

            var otp = await _context.LoginOtps
                .Where(o => o.UserId == user.UserId && o.Purpose == "PasswordReset" && !o.IsUsed)
                .OrderByDescending(o => o.RequestedAt)
                .FirstOrDefaultAsync();

            if (otp == null || otp.ExpiresAt < DateTime.UtcNow)
                return Ok(new { success = false, message = "کد تأیید منقضی شده است. دوباره درخواست دهید." });

            if (otp.AttemptCount >= otp.MaxAttempts)
            {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
                return Ok(new { success = false, message = "تعداد تلاش‌های مجاز به پایان رسید. دوباره کد بگیرید." });
            }

            if (otp.OtpCodeHash != _encryptionHelper.ComputeSearchHash(model.Code))
            {
                otp.AttemptCount += 1;
                await _context.SaveChangesAsync();
                return Ok(new { success = false, message = "کد تأیید اشتباه است." });
            }

            otp.IsVerified = true;
            otp.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var expiry = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
            var payload = $"{user.UserId}|{otp.OtpId}|{expiry}";
            var payloadEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            var signature = _encryptionHelper.SignPayload(payload);
            var resetToken = $"{payloadEncoded}.{signature}";

            await LogAction(user.UserId, "ForgotPassword_CodeVerified", user.Username, "کد بازیابی رمز عبور تأیید شد");

            return Ok(new { success = true, message = "کد با موفقیت تأیید شد.", resetToken });
        }

        [HttpPost("forgot-password/reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetForgotPassword([FromBody] ForgotPasswordResetRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.ResetToken))
                return Ok(new { success = false, message = "درخواست نامعتبر است." });

            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword != model.ConfirmNewPassword)
                return Ok(new { success = false, message = "رمز عبور و تأیید آن یکسان نیستند." });

            if (!Regex.IsMatch(model.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"))
                return Ok(new { success = false, message = "رمز عبور باید حداقل ۸ کاراکتر و شامل حرف بزرگ، حرف کوچک، عدد و نویسه خاص باشد." });

            var parts = model.ResetToken.Split('.', 2);
            if (parts.Length != 2)
                return Ok(new { success = false, message = "توکن نامعتبر است." });

            string payload;
            try
            {
                payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            }
            catch
            {
                return Ok(new { success = false, message = "توکن نامعتبر است." });
            }

            if (!_encryptionHelper.VerifyPayload(payload, parts[1]))
                return Ok(new { success = false, message = "توکن نامعتبر است." });

            var segments = payload.Split('|');
            if (segments.Length != 3 ||
                !long.TryParse(segments[0], out var userId) ||
                !long.TryParse(segments[1], out var otpId) ||
                !long.TryParse(segments[2], out var expiryUnix))
            {
                return Ok(new { success = false, message = "توکن نامعتبر است." });
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiryUnix)
                return Ok(new { success = false, message = "توکن منقضی شده است. فرآیند را از ابتدا انجام دهید." });

            var otp = await _context.LoginOtps.FirstOrDefaultAsync(o =>
                o.OtpId == otpId && o.UserId == userId && o.Purpose == "PasswordReset");
            if (otp == null || !otp.IsVerified || otp.IsUsed)
                return Ok(new { success = false, message = "توکن نامعتبر است یا قبلاً استفاده شده." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Ok(new { success = false, message = "کاربر یافت نشد." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            await LogAction(user.UserId, "ForgotPassword_Reset_Success", user.Username, "رمز عبور با موفقیت بازنشانی شد");

            return Ok(new { success = true, message = "رمز عبور با موفقیت تغییر کرد." });
        }

        #endregion

        private bool IsAdmin()
        {
            var roleIdClaim = User.FindFirst("RoleId")?.Value;

            if (int.TryParse(roleIdClaim, out var roleId) && roleId == 3) return true;
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
            return roleName == "ادمین";
        }


        [HttpGet("GetUsers")]
        [Authorize] // تغییر از AllowAnonymous به Authorize چون IsAdmin نیاز به Claims دارد
        public async Task<ActionResult<IEnumerable<UserViewModel>>> GetUsers()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }
            var users = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Select(u => new UserViewModel
                {
                    UserId = u.UserId,
                    NationalId = u.NationalId,
                    Username = u.Username,
                    Name = u.Name,
                    LastName = u.LastName,
                    Role = u.UserRoles.Select(ur => ur.Role != null ? ur.Role.RoleName : null).FirstOrDefault() ?? "بدون نقش",
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    IsActive = u.IsActive,
                    MobileNumber = u.MobileNumber
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("UpdateUser/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserRequest model)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("کاربر یافت نشد.");

            // فقط فیلدهایی که مقدار دارند را به‌روزرسانی کن
            if (!string.IsNullOrEmpty(model.Name))
                user.Name = model.Name;

            if (!string.IsNullOrEmpty(model.LastName))
                user.LastName = model.LastName;

            if (!string.IsNullOrEmpty(model.Username))
                user.Username = model.Username;

            if (!string.IsNullOrEmpty(model.NationalId))
                user.NationalId = model.NationalId;

            if (!string.IsNullOrEmpty(model.MobileNumber))
                user.MobileNumber = model.MobileNumber;

            if (!string.IsNullOrEmpty(model.Extension))
                user.Extension = model.Extension;

            // RoleId از طریق UserRoles مدیریت شود
            if (model.RoleId.HasValue && model.RoleId.Value > 0)
            {
                // حذف نقش‌های قبلی و اضافه کردن جدید (ساده)
                var existingRoles = await _context.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
                _context.UserRoles.RemoveRange(existingRoles);

                _context.UserRoles.Add(new UserRole
                {
                    UserId = id,
                    RoleId = model.RoleId.Value,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // اگر رمز جدید فرستاده شده بود، بروزرسانی کن
            if (!string.IsNullOrEmpty(model.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            await _context.SaveChangesAsync();
            await LogAction(user.UserId, "UpdateUser_Success", user.Username, "User updated successfully");

            return Ok(new { success = true, message = "اطلاعات کاربر با موفقیت بروزرسانی شد." });
        }

        // 🔴 Soft Delete (غیرفعال کردن کاربر)
        [HttpDelete("SoftDeleteUser/{id}")]
        [Authorize]
        public async Task<IActionResult> SoftDeleteUser(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("کاربر یافت نشد.");

            if (!user.IsActive)
                return BadRequest("کاربر از قبل غیرفعال است.");

            user.IsActive = false;
            await _context.SaveChangesAsync();
            await LogAction(user.UserId, "SoftDeleteUser", user.Username, "User deactivated");

            return Ok("کاربر با موفقیت غیرفعال شد.");
        }

        // 🟢 فعال‌سازی مجدد کاربر
        [HttpPost("RestoreUser/{id}")]
        [Authorize]
        public async Task<IActionResult> RestoreUser(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("کاربر یافت نشد.");

            if (user.IsActive)
                return BadRequest("کاربر از قبل فعال است.");

            user.IsActive = true;
            await _context.SaveChangesAsync();
            await LogAction(user.UserId, "RestoreUser", user.Username, "User restored");

            return Ok("کاربر با موفقیت فعال شد.");
        }

        // ⚫ حذف واقعی از دیتابیس (اختیاری)
        [HttpDelete("HardDeleteUser/{id}")]
        [Authorize]
        public async Task<IActionResult> HardDeleteUser(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("کاربر یافت نشد.");

            // حذف نقش‌ها ابتدا
            var roles = await _context.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
            _context.UserRoles.RemoveRange(roles);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await LogAction(id, "HardDeleteUser", user.Username, "User permanently deleted");

            return Ok("کاربر به صورت دائم حذف شد.");
        }


        [HttpPost("grant-access")]
        [Authorize(Policy = "CanManageAccess")]
        public async Task<IActionResult> GrantAccess([FromBody] GrantAccessViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
                return NotFound("کاربر یافت نشد.");

            var callerUserId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (callerUserId == 0)
                return Unauthorized("کاربر شناسایی نشد.");

            var isAdmin = await _context.UserAccesses
                .AnyAsync(ua => ua.UserId == callerUserId && ua.Permission == "CanManageAccess");
            if (!isAdmin)
                return Forbid("شما دسترسی لازم برای مدیریت دسترسی‌ها را ندارید.");

            if (await _context.UserAccesses.AnyAsync(ua => ua.UserId == model.UserId && ua.Permission == model.Permission))
                return BadRequest("این دسترسی قبلاً برای کاربر ثبت شده است.");

            var userAccess = new UserAccess
            {
                UserId = model.UserId,
                Permission = model.Permission
            };
            _context.UserAccesses.Add(userAccess);
            await _context.SaveChangesAsync();

            await LogAction(model.UserId, "GrantAccess_Success", user.Username, $"Permission {model.Permission} granted");
            return Ok($"دسترسی {model.Permission} به کاربر {user.Username} اعطا شد.");
        }

        [HttpDelete("revoke-access")]
        [Authorize(Policy = "CanManageAccess")]
        public async Task<IActionResult> RevokeAccess([FromBody] GrantAccessViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var callerUserId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (callerUserId == 0)
                return Unauthorized("کاربر شناسایی نشد.");

            var isAdmin = await _context.UserAccesses
                .AnyAsync(ua => ua.UserId == callerUserId && ua.Permission == "CanManageAccess");
            if (!isAdmin)
                return Forbid("شما دسترسی لازم برای مدیریت دسترسی‌ها را ندارید.");

            var userAccess = await _context.UserAccesses
                .FirstOrDefaultAsync(ua => ua.UserId == model.UserId && ua.Permission == model.Permission);
            if (userAccess == null)
                return NotFound("دسترسی یافت نشد.");

            _context.UserAccesses.Remove(userAccess);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(model.UserId);
            await LogAction(model.UserId, "RevokeAccess_Success", user?.Username ?? "Unknown", $"Permission {model.Permission} revoked");
            return Ok($"دسترسی {model.Permission} از کاربر {user?.Username ?? "Unknown"} حذف شد.");
        }

        [HttpPost("refresh/revoke")]
        [Authorize]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshRequestViewModel model)
        {
            if (string.IsNullOrEmpty(model.RefreshToken))
                return BadRequest("Refresh Token ارائه نشده است.");

            var result = await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);

            var callerId = User.FindFirst("UserId")?.Value;
            long.TryParse(callerId, out long callerUserId);
            await LogAction(callerUserId, "RevokeRefreshToken", User.Identity?.Name, "Refresh token revoked");

            return Ok(result);
        }

        [HttpPost("revoke-all")]
        [Authorize]
        public async Task<IActionResult> RevokeAllRefreshTokens([FromBody] RevokeAllRequestViewModel model)
        {
            var result = await _tokenService.RevokeAllRefreshTokensAsync(model.UserId);

            var callerId = User.FindFirst("UserId")?.Value;
            long.TryParse(callerId, out long callerUserId);
            await LogAction(callerUserId, "RevokeAllRefreshTokens", User.Identity?.Name, $"TargetUserId={model.UserId}");

            return Ok(result);
        }

        public class RevokeAllRequestViewModel
        {
            public long UserId { get; set; }
        }

        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                return Unauthorized("کاربر شناسایی نشد.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound("کاربر یافت نشد.");

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                return BadRequest("رمز عبور فعلی اشتباه است.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            await LogAction(userId, "ChangePassword_Success", user.Username, "Password changed successfully");
            return Ok("رمز عبور با موفقیت تغییر کرد.");
        }

        [HttpGet("GetCurrentUser")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("کاربر شناسایی نشد.");
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound("کاربر یافت نشد.");
                }

                var roleName = user.UserRoles?.FirstOrDefault()?.Role?.RoleName ?? "بدون نقش";

                var userInfo = new
                {
                    user.UserId,
                    user.Username,
                    user.Name,
                    user.LastName,
                    Role = roleName
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                await LogAction(0, "GetCurrentUser_Error", null, ex.Message);
                return StatusCode(500, "خطا در سرور: " + ex.Message);
            }
        }

        [HttpGet("GetRoles")]
        [Authorize]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new
                    {
                        roleId = r.RoleId,
                        roleName = r.RoleName
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"خطا در دریافت نقش‌ها: {ex.Message}" });
            }
        }

        private async Task LogAction(long userId, string action, string? username, string result)
        {
            try
            {
                // UserId در DB NOT NULL است، اگر 0 باشد ممکن است مشکل ایجاد کند
                if (userId <= 0) userId = 1; // fallback موقت

                _context.UserLogs.Add(new UserLog
                {
                    UserId = userId,
                    Action = $"{action}: Username={username}, Result={result}",
                    ActionTime = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    ActionResult = result,
                    LogLevel = "Info"
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving UserLog: {ex}");
            }
        }
    }

    public class RefreshRequestViewModel
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordIdentityRequest
    {
        public string? NationalId { get; set; }
        public string? MobileNumber { get; set; }
    }

    public class ForgotPasswordVerifyRequest
    {
        public string? NationalId { get; set; }
        public string? MobileNumber { get; set; }
        public string? Code { get; set; }
    }

    public class ForgotPasswordResetRequest
    {
        public string? ResetToken { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
    }
}