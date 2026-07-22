using DNTCaptcha.Core;
using IdentityManagementSystem.UI.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;

namespace IdentityManagementSystem.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDNTCaptchaValidatorService _captchaValidatorService;
        private readonly HttpClient _client;

        public HomeController(
            IHttpClientFactory httpClientFactory,
            IDNTCaptchaValidatorService captchaValidatorService)
        {
            _client = httpClientFactory.CreateClient("PomixApi");
            _captchaValidatorService = captchaValidatorService ?? throw new ArgumentNullException(nameof(captchaValidatorService));
        }

        #region Login
        [HttpGet]
        public IActionResult LoginPage()
        {
            return View(new LoginViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginPage(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "لطفاً همه فیلدها را وارد کنید.";
                return View(model);
            }

            try
            {
                var response = await _client.PostAsJsonAsync("auth/login", model);
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    if (loginResponse?.Tokens?.AccessToken != null)
                    {
                        HttpContext.Session.SetString("JwtToken", loginResponse.Tokens.AccessToken);
                        HttpContext.Session.SetString("RefreshToken", loginResponse.Tokens.RefreshToken ?? "");

                        // ذخیره اطلاعات نقش کاربر در Session - دسترسی مستقیم به properties
                        var roleName = loginResponse.Role?.RoleName ?? "";
                        var roleId = loginResponse.Role?.RoleId.ToString() ?? "0";

                        HttpContext.Session.SetString("UserRole", roleName);
                        HttpContext.Session.SetString("UserRoleId", roleId);
                        HttpContext.Session.SetString("UserName", loginResponse.Name ?? "");
                        HttpContext.Session.SetString("UserLastName", loginResponse.LastName ?? "");
                        HttpContext.Session.SetString("UserId", loginResponse.UserId.ToString());
                        HttpContext.Session.SetString("Username", loginResponse.Username ?? "");

                        return RedirectToAction("Index", "Cartable");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "خطا: توکن دریافت نشد.";
                        return View(model);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ViewBag.ErrorMessage = "خطا در ورود: " + error;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "خطا در ارتباط با سرور: " + ex.Message;
                return View(model);
            }
        }
        #endregion

        #region Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                var refreshToken = HttpContext.Session.GetString("RefreshToken");

                HttpContext.Session.Remove("JwtToken");
                HttpContext.Session.Remove("RefreshToken");
                HttpContext.Session.Clear();

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken))
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await _client.PostAsJsonAsync("auth/refresh/revoke", new { RefreshToken = refreshToken });
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to revoke refresh token: {await response.Content.ReadAsStringAsync()}");
                    }
                }

                TempData["SuccessLogoutMessage"] = "شما با موفقیت از سیستم خارج شدید.";
                return RedirectToAction("LoginPage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                ViewBag.ErrorMessage = "خطا در خروج از سیستم: " + ex.Message;
                return RedirectToAction("LoginPage");
            }
        }
        #endregion

        #region Users
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    TempData["ErrorMessage"] = "لطفاً ابتدا وارد سیستم شوید.";
                    return RedirectToAction("LoginPage");
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.GetAsync("Auth/GetUsers");
                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<UserViewModel>>();
                    return View(users ?? new List<UserViewModel>());
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"خطا در دریافت کاربران: {error}";
                    return View(new List<UserViewModel>());
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"خطا در ارتباط با سرور: {ex.Message}";
                return View(new List<UserViewModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(" | ", errors) });
                }

                if (model.Password != model.ConfirmPassword)
                {
                    return Json(new { success = false, message = "رمز عبور و تأیید رمز عبور یکسان نیستند" });
                }

                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید" });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.PostAsJsonAsync("Auth/register", new
                {
                    model.Name,
                    model.LastName,
                    model.Username,
                    model.Password,
                    model.NationalId,
                    model.MobileNumber,
                    model.RoleId
                });

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "کاربر با موفقیت ایجاد شد" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"خطا در ایجاد کاربر: {errorContent}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(long id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.GetAsync($"Auth/GetUsers");
                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<UserViewModel>>();
                    var user = users?.FirstOrDefault(u => u.UserId == id);
                    if (user == null)
                    {
                        return Json(new { success = false, message = "کاربر یافت نشد." });
                    }
                    return Json(new
                    {
                        success = true,
                        userId = user.UserId,
                        name = user.Name,
                        lastName = user.LastName,
                        username = user.Username,
                        nationalId = user.NationalId,
                        mobileNumber = user.MobileNumber,
                        roleId = user.RoleId,
                        isActive = user.IsActive
                    });
                }
                return Json(new { success = false, message = "خطا در دریافت اطلاعات کاربر" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(UpdateUserViewModel model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.Password) && model.Password != model.ConfirmPassword)
                {
                    return Json(new { success = false, message = "رمز عبور و تأیید رمز عبور یکسان نیستند" });
                }

                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید" });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // ساختن آبجکت برای ارسال به API
                var updateData = new
                {
                    model.Name,
                    model.LastName,
                    model.Username,
                    Password = string.IsNullOrEmpty(model.Password) ? null : model.Password,
                    model.NationalId,
                    model.MobileNumber,
                    model.RoleId
                };

                var response = await _client.PutAsJsonAsync($"Auth/UpdateUser/{model.UserId}", updateData);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "کاربر با موفقیت به‌روزرسانی شد" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"خطا در به‌روزرسانی کاربر: {errorContent}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteUser(long id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "توکن یافت نشد. لطفاً دوباره وارد سیستم شوید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.DeleteAsync($"Auth/SoftDeleteUser/{id}");

                if (response.IsSuccessStatusCode)
                {
                    // خواندن پیام از پاسخ API
                    var result = await response.Content.ReadAsStringAsync();
                    return Json(new { success = true, message = result ?? "کاربر با موفقیت غیرفعال شد." });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"خطا در غیرفعال کردن کاربر: {error}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreUser(long id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید" });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.PostAsync($"Auth/RestoreUser/{id}", null);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "کاربر با موفقیت فعال شد" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"خطا در فعال‌سازی کاربر: {errorContent}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "توکن یافت نشد." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.GetAsync("Auth/GetCurrentUser");

                if (response.IsSuccessStatusCode)
                {
                    var userInfo = await response.Content.ReadFromJsonAsync<UserProfileViewModel>();
                    return Json(new
                    {
                        success = true,
                        name = userInfo.Name,
                        lastName = userInfo.LastName,
                        role = userInfo.Role
                    });
                }

                return Json(new { success = false, message = "خطا در دریافت اطلاعات کاربر" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                    return Unauthorized(); // یا return Json(new List<RoleViewModel>());

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.GetAsync("Auth/GetRoles");

                if (response.IsSuccessStatusCode)
                {
                    var roles = await response.Content.ReadFromJsonAsync<List<RoleViewModel>>();
                    return Ok(roles); // فقط لیست رو برگردون
                }

                return Ok(new List<RoleViewModel>()); // لیست خالی
            }
            catch (Exception ex)
            {
                return Ok(new List<RoleViewModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "لطفاً همه فیلدها را به درستی پر کنید." });
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                return Json(new { success = false, message = "رمز عبور جدید و تأیید رمز عبور یکسان نیستند." });
            }

            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید." });
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PostAsJsonAsync("Auth/ChangePassword", model);
            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "رمز عبور با موفقیت تغییر کرد." });
            }

            var error = await response.Content.ReadAsStringAsync();
            var errorObj = JsonConvert.DeserializeObject<dynamic>(error);
            return Json(new { success = false, message = errorObj?.message ?? $"خطا در تغییر رمز عبور: {error}" });
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userInfo = new UserInfo
            {
                UserId = long.Parse(HttpContext.Session.GetString("UserId") ?? "0"),
                Username = HttpContext.Session.GetString("Username") ?? "",
                Name = HttpContext.Session.GetString("UserName") ?? "",
                LastName = HttpContext.Session.GetString("UserLastName") ?? "",
                Role = HttpContext.Session.GetString("UserRole") ?? "بدون نقش"
            };

            if (userInfo.UserId == 0)
            {
                ViewBag.ErrorMessage = "لطفاً ابتدا وارد سیستم شوید.";
                return RedirectToAction("LoginPage");
            }

            return View(userInfo);
        }

        #endregion
    }

    #region ViewModels
    public class ChangePasswordViewModel
    {
        public string? CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }

    public class UserProfileViewModel
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
    }

    public class RoleInfo
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class LoginResponse
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public RoleInfo Role { get; set; }
        public TokenInfo Tokens { get; set; }
    }

    public class UserInfo
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }  
    }

    public class RoleViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class TokenInfo
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class UpdateUserViewModel
    {
        public long UserId { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string NationalId { get; set; }
        public string MobileNumber { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }
    #endregion
}