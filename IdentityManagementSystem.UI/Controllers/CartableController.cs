using DNTCaptcha.Core;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using IdentityManagementSystem.UI.Filters;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using IdentityManagementSystem.UI.Enums;

using System.Text.Json;
using System.Text.RegularExpressions;
using JsonSerializer = System.Text.Json.JsonSerializer;
using IdentityManagementSystem.UI.Helpers;
using IdentityManagementSystem.Shared.DTO;

namespace IdentityManagementSystem.UI.Controllers
{
    [NoCacheFilter]
    public class CartableController : Controller
    {
        private readonly HttpClient _client;
        private readonly IDNTCaptchaValidatorService _captchaValidatorService;
        private readonly ILogger<CartableController> _logger;
        private readonly IConfiguration _configuration;

        // کش ساده‌ی توکن حساب سرویسی «پورتال عمومی» — بین درخواست‌های anonymous مختلف به اشتراک می‌ره
        // تا هر ثبت درخواست مجبور به لاگین دوباره نباشه. Controller instance جدید ولی static field مشترکه.
        private static string? _publicPortalTokenCache;
        private static DateTime _publicPortalTokenExpiresAt;
        private static readonly SemaphoreSlim _publicPortalTokenLock = new(1, 1);

        public CartableController(
            IHttpClientFactory httpClientFactory,
            IDNTCaptchaValidatorService captchaValidatorService,
            ILogger<CartableController> logger,
            IConfiguration configuration)
        {
            _client = httpClientFactory.CreateClient("PomixApi");
            _captchaValidatorService = captchaValidatorService ?? throw new ArgumentNullException(nameof(captchaValidatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// توکن حساب سرویسی «پورتال عمومی» (Bsr:PublicPortal تو appsettings) — برای متقاضی‌هایی که
        /// بدون لاگین (فقط با تایید OTP) درخواست ثبت می‌کنن. کش می‌شه تا نیازی به لاگین مکرر نباشه.
        /// </summary>
        private async Task<string?> GetPublicPortalTokenAsync()
        {
            if (!string.IsNullOrEmpty(_publicPortalTokenCache) && DateTime.UtcNow < _publicPortalTokenExpiresAt)
            {
                return _publicPortalTokenCache;
            }

            await _publicPortalTokenLock.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_publicPortalTokenCache) && DateTime.UtcNow < _publicPortalTokenExpiresAt)
                {
                    return _publicPortalTokenCache;
                }

                var username = _configuration["PublicPortal:Username"];
                var password = _configuration["PublicPortal:Password"];
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("PublicPortal:Username/Password در appsettings تنظیم نشده — ثبت درخواست anonymous ممکن نیست.");
                    return null;
                }

                var response = await _client.PostAsJsonAsync("auth/login", new { Username = username, Password = password });
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("لاگین حساب سرویسی پورتال عمومی ناموفق بود: {StatusCode}", response.StatusCode);
                    return null;
                }

                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse?.Tokens?.AccessToken == null)
                {
                    _logger.LogError("پاسخ لاگین حساب سرویسی پورتال عمومی فاقد توکن بود.");
                    return null;
                }

                _publicPortalTokenCache = loginResponse.Tokens.AccessToken;
                // کمی زودتر از انقضای واقعی (۱۵ دقیقه) منقضی در نظر می‌گیریم تا وسط یه درخواست نخوره
                _publicPortalTokenExpiresAt = DateTime.UtcNow.AddMinutes(12);
                return _publicPortalTokenCache;
            }
            finally
            {
                _publicPortalTokenLock.Release();
            }
        }

        #region CartableIndex

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string search = "", string filterStatus = "", string? fromDate = null, string? toDate = null)
        {
            // ========== چک نقش ==========
            int roleId = 0;

            var roleIdClaim = User.FindFirst("RoleId")?.Value;
            if (!string.IsNullOrEmpty(roleIdClaim))
            {
                int.TryParse(roleIdClaim, out roleId);
            }
            else if (HttpContext.Session.GetInt32("RoleId").HasValue)
            {
                roleId = HttpContext.Session.GetInt32("RoleId").Value;
            }

            // اگر متقاضی بود → بفرست به ClientIndex
            if (roleId == 2)
            {
                return RedirectToAction("ClientIndex", new { page, search });
            }
            // ============================

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            // گرفتن داده‌ها از API
            var model = await GetCartableData(page, search, filterStatus, fromDate, toDate);

            // بررسی خوانده شدن سندها
            foreach (var item in model.Items)
            {
                try
                {
                    var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                    if (!string.IsNullOrEmpty(token))
                        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _client.GetAsync($"Service/GetTextByRequestId/{item.RequestId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = await response.Content.ReadFromJsonAsync<DocTextResponse>();
                        item.IsRead = apiResponse?.IsRead ?? false;
                    }
                    else
                    {
                        item.IsRead = false;
                    }
                }
                catch (Exception)
                {
                    item.IsRead = false;
                }
            }

            ViewBag.FormModel = new CartableFormViewModel();
            ViewBag.FilterStatus = filterStatus;
            ViewBag.RejectReasons = EnumHelper.ToSelectList<RejectReason>();

            return View(model);
        }


        /// <summary>
        /// چک تطابق کد ملی/شماره موبایل (شاهکار) — قبل از ارسال کد تایید صدا زده می‌شه.
        /// اگه تطابق نداشت، فرانت اصلاً SendOtp رو صدا نمی‌زنه.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CheckShahkar(string nationalCode, string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(nationalCode) || nationalCode.Length != 10)
                return Json(new { success = false, message = "کد ملی نامعتبر است" });

            if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length != 11 || !mobileNumber.StartsWith("09"))
                return Json(new { success = false, message = "شماره موبایل نامعتبر است" });

            try
            {
                var token = HttpContext.Session.GetString("JwtToken");
                if (string.IsNullOrEmpty(token))
                {
                    token = await GetPublicPortalTokenAsync();
                }
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "سامانه موقتاً در دسترس نیست. لطفاً کمی بعد دوباره تلاش کنید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.PostAsJsonAsync("Service/CheckShahkarMatch", new { NationalId = nationalCode, MobileNumber = mobileNumber });
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CheckShahkarMatch returned {StatusCode}: {Content}", response.StatusCode, content);
                    return Json(new { success = false, message = "خطا در ارتباط با سرویس احراز هویت." });
                }

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در چک شاهکار برای کد ملی {NationalCode}", nationalCode);
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult SendOtp(string nationalCode, string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(nationalCode) || nationalCode.Length != 10)
                return Json(new { success = false, message = "کد ملی نامعتبر است" });

            if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length != 11 || !mobileNumber.StartsWith("09"))
                return Json(new { success = false, message = "شماره موبایل نامعتبر است" });

            var otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString($"OTP_{mobileNumber}", otp);
            HttpContext.Session.SetString($"OTP_Time_{mobileNumber}", DateTime.UtcNow.ToString("O"));

            _logger.LogWarning("===== OTP برای تست: {Otp} | موبایل: {Mobile} =====", otp, mobileNumber);

            return Json(new
            {
                success = true,
                message = "کد تایید ارسال شد",
                debugOtp = otp   // فقط برای تست
            });
        }

        [HttpPost]
        public IActionResult VerifyOtp(string mobileNumber, string otpCode)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber) || string.IsNullOrWhiteSpace(otpCode))
                return Json(new { success = false, message = "اطلاعات ناقص است" });

            var savedOtp = HttpContext.Session.GetString($"OTP_{mobileNumber}");
            var timeStr = HttpContext.Session.GetString($"OTP_Time_{mobileNumber}");

            if (string.IsNullOrEmpty(savedOtp))
                return Json(new { success = false, message = "کدی ارسال نشده یا منقضی شده است" });

            if (DateTime.TryParse(timeStr, out var sentTime))
            {
                if ((DateTime.UtcNow - sentTime).TotalMinutes > 2)
                {
                    HttpContext.Session.Remove($"OTP_{mobileNumber}");
                    HttpContext.Session.Remove($"OTP_Time_{mobileNumber}");
                    return Json(new { success = false, message = "کد منقضی شده است. دوباره ارسال کنید" });
                }
            }

            if (savedOtp != otpCode)
                return Json(new { success = false, message = "کد تایید اشتباه است" });

            HttpContext.Session.Remove($"OTP_{mobileNumber}");
            HttpContext.Session.Remove($"OTP_Time_{mobileNumber}");
            HttpContext.Session.SetString($"OTP_Verified_{mobileNumber}", "true");

            return Json(new { success = true, message = "شماره موبایل با موفقیت تایید شد" });
        }

        // ClientIndex/TrackRequest کوچ کردن به IdentityManagementSystem.PublicPortal —
        // متقاضی دیگه از این پروژه (داخلی) به این صفحات دسترسی نداره؛ پروژه‌ی عمومی جداگانه‌ست.
        // اگه یه حساب متقاضی قدیمی (RoleId=2) اینجا لاگین کنه، Index() هنوز سعی می‌کنه بهش
        // ریدایرکت بده — که دیگه وجود نداره (404). این یه edge case شناخته‌شده و بی‌خطره،
        // چون ثبت‌نام متقاضی جدید دیگه از این مسیر انجام نمی‌شه.

        /// <summary>
        /// داشبورد مخصوص متقاضی (RoleId = 2)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ApplicantIndex(int page = 1, string search = "")
        {
            var roleId = HttpContext.Session.GetInt32("RoleId") ?? 0;
            if (roleId != 2)
            {
                // اگر کارشناس یا ادمین اومد اینجا، بفرستش به کارتابل اصلی
                return RedirectToAction(nameof(Index));
            }

            // فعلاً از همون متد GetCartableData استفاده می‌کنیم
            // (بعداً می‌تونی فیلتر بر اساس کاربر لاگین‌شده اضافه کنی)
            var model = await GetCartableData(page, search, filterStatus: "");

            ViewBag.FormModel = new CartableFormViewModel();

            return View("ApplicantIndex", model);   // ویوی متقاضی که قبلاً ساختم
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateSteps(CartableFormViewModel model)
        {
            // اعتبارسنجی فرمت پایه — این فقط یه گیت جلوی داده‌ی واضحاً غلط هست،
            // نه احراز هویت واقعی. تصمیم نهایی (تایید/رد) رو سرویس‌ها و متصدی می‌گیرن.
            var formatErrors = new List<string>();
            if (string.IsNullOrEmpty(model.NationalCode) || !Regex.IsMatch(model.NationalCode, @"^\d{10}$"))
                formatErrors.Add("کد ملی نامعتبر است.");
            if (string.IsNullOrEmpty(model.MobileNumber) || !Regex.IsMatch(model.MobileNumber, @"^09\d{9}$"))
                formatErrors.Add("شماره موبایل نامعتبر است.");
            if (string.IsNullOrEmpty(model.DocumentNumber) || !Regex.IsMatch(model.DocumentNumber, @"^\d{18}$"))
                formatErrors.Add("شناسه سند نامعتبر است.");
            if (string.IsNullOrEmpty(model.VerifyCode) || !Regex.IsMatch(model.VerifyCode, @"^\d{6}$"))
                formatErrors.Add("رمز تصدیق نامعتبر است.");
            if (string.IsNullOrWhiteSpace(model.WarehouseReceiptNumber))
                formatErrors.Add("شماره قبض انبار الزامی است.");

            if (formatErrors.Count > 0)
            {
                return Json(new { success = false, message = string.Join(" ", formatErrors) });
            }

            // تایید OTP الزامیه — چه متقاضی anonymous باشه چه کارشناس/متصدیِ لاگین‌کرده.
            // این چک سمت سرور انجام می‌شه (نه فقط JS) چون کسی می‌تونه مستقیم این endpoint رو صدا بزنه.
            var otpVerified = HttpContext.Session.GetString($"OTP_Verified_{model.MobileNumber}") == "true";
            if (!otpVerified)
            {
                return Json(new { success = false, message = "لطفاً ابتدا شماره موبایل متقاضی را با کد پیامکی تایید کنید." });
            }

            var sessionToken = HttpContext.Session.GetString("JwtToken");
            string? token;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                // کاربر لاگین‌کرده (متصدی، یا حساب متقاضی قدیمی) — از توکن خودش استفاده می‌شه
                token = sessionToken;
            }
            else
            {
                // مسیر متقاضی anonymous — بدون لاگین
                token = await GetPublicPortalTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "سامانه موقتاً در دسترس نیست. لطفاً کمی بعد دوباره تلاش کنید." });
                }
            }

            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.PostAsJsonAsync("Service/ProcessCombinedRequest", new CombinedRequestViewModel
                {
                    NationalId = model.NationalCode,
                    MobileNumber = model.MobileNumber,
                    DocumentNumber = model.DocumentNumber,
                    VerificationCode = model.VerifyCode,
                    WarehouseReceiptNumber = model.WarehouseReceiptNumber
                });

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ProcessCombinedRequest returned {StatusCode}: {Content}", response.StatusCode, content);
                    return Json(new { success = false, message = "خطا در ارتباط با سرویس. لطفاً دوباره تلاش کنید." });
                }

                dynamic result = JsonConvert.DeserializeObject<dynamic>(content);
                bool apiSuccess = result?.success == true;

                if (!apiSuccess)
                {
                    string apiMessage = (string?)result?.message ?? "درخواست ثبت نشد.";
                    return Json(new { success = false, message = apiMessage });
                }

                // درخواست تو دیتابیس ثبت شده — صرف‌نظر از نتیجه‌ی تطابق. جزئیات وضعیت رو
                // متقاضی/متصدی از ستون «وضعیت» تو جدول کارتابل می‌بینن، نه از این پیام.
                bool? isMatch = null;
                if (result?.data?.Shahkar != null)
                {
                    isMatch = (bool)(result.data.Shahkar.IsSuccessful == true);
                }

                string? trackingCode = (string?)result?.data?.TrackingCode;

                string message = !string.IsNullOrEmpty(trackingCode)
                    ? $"درخواست شما با کد پیگیری {trackingCode} ثبت شد."
                    : (isMatch == false
                        ? "درخواست شما ثبت شد. وضعیت آن را از جدول درخواست‌ها دنبال کنید."
                        : "درخواست شما با موفقیت ثبت شد.");

                return Json(new { success = true, message, trackingCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ثبت درخواست جدید برای کد ملی {NationalCode}", model.NationalCode);
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(CartableFormViewModel model)
        {
            // کدهای قبلی validation (کپچا و شرایط) بدون تغییر باقی می‌ماند
            if (!_captchaValidatorService.HasRequestValidCaptchaEntry())
            {
                ViewBag.ErrorMessage = "کد امنیتی اشتباه است.";
                ViewBag.FormModel = model;
                return View("Index", await GetCartableData(1, "", ""));
            }

            if (!model.AgreeToTerms)
            {
                ViewBag.ErrorMessage = "لطفاً با شرایط موافقت کنید.";
                ViewBag.FormModel = model;
                return View("Index", await GetCartableData(1, "", ""));
            }

            try
            {
                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (string.IsNullOrEmpty(token))
                {
                    ViewBag.ErrorMessage = "لطفاً ابتدا وارد سیستم شوید.";
                    return RedirectToAction("LoginPage", "Home");
                }
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var requestData = new CombinedRequestViewModel
                {
                    NationalId = model.ClientNationalCode,
                    MobileNumber = model.MobileNumber,
                    DocumentNumber = model.DocumentNumber,
                    VerificationCode = model.VerifyCode,
                    WarehouseReceiptNumber = model.WarehouseReceiptNumber
                };

                var response = await _client.PostAsJsonAsync("Service/ProcessCombinedRequest", requestData);

                if (response.IsSuccessStatusCode)
                {
                    // ✅ دریافت پاسخ و ثبت موفقیت‌آمیز
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // فقط برای اطمینان از دریافت داده‌ها لاگ بگیرید
                    _logger.LogInformation("API Response: {Response}", responseContent);

                    ViewBag.SuccessMessage = "درخواست با موفقیت ثبت شد و به کارتابل اضافه گردید.";
                    ViewBag.FormModel = new CartableFormViewModel();

                    // رفرش کردن داده‌های کارتابل
                    return View("Index", await GetCartableData(1, "", ""));
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ViewBag.ErrorMessage = $"خطا در ثبت درخواست: {error}";
                    ViewBag.FormModel = model;
                    return View("Index", await GetCartableData(1, "", ""));
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"خطا در ارتباط با سرور: {ex.Message}";
                ViewBag.FormModel = model;
                return View("Index", await GetCartableData(1, "", ""));
            }
        }


        public class UpdateValidationStatusModel
        {
            public long RequestId { get; set; }
            public bool ValidateByExpert { get; set; }
            public string? Description { get; set; } 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateValidationStatus([FromBody] UpdateValidationStatusModel model)
        {
            try
            {
                _logger.LogInformation("UpdateValidationStatus called for RequestId: {RequestId}, ValidateByExpert: {ValidateByExpert}",
                    model.RequestId, model.ValidateByExpert);

                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token not found for UpdateValidationStatus");
                    return Json(new { success = false, message = "توکن یافت نشد. لطفاً دوباره وارد سیستم شوید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var apiModel = new
                {
                    RequestId = model.RequestId,
                    ValidateByExpert = model.ValidateByExpert,
                    Description = model.ValidateByExpert == false ? model.Description : null // فقط برای رد
                };

                var json = System.Text.Json.JsonSerializer.Serialize(apiModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to API: {Json}", json);

                var response = await _client.PostAsync("Request/UpdateValidationStatus", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response - Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse>(responseContent);
                        return Json(new { success = true, message = apiResponse?.Message ?? "وضعیت با موفقیت به‌روز شد." });
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parsing API response");
                        return Json(new { success = true, message = "وضعیت با موفقیت به‌روز شد." });
                    }
                }
                else
                {
                    try
                    {
                        // ✅ تنظیم برای نادیده گرفتن تفاوت حروف بزرگ و کوچک
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse>(responseContent, options);

                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            // 🔥 دریافت دقیق پیام خطا از API
                            string errorMessage = errorResponse?.Message ?? responseContent ?? "عملیات ناموفق بود";

                            if (errorMessage.Contains("سند وجود ندارد"))
                            {
                                return Json(new { success = false, message = "❌ سند وجود ندارد. نمی‌توانید تأیید کنید." });
                            }
                            else if (errorMessage.Contains("سند را بررسی"))
                            {
                                return Json(new { success = false, message = "❌ ابتدا باید سند را مطالعه و تیک 'سند مشاهده شد' را بزنید." });
                            }
                            else
                            {
                                return Json(new { success = false, message = $"❌ {errorMessage}" });
                            }
                        }
                        else
                        {
                            return Json(new
                            {
                                success = false,
                                message = errorResponse?.Message ?? $"خطا از سمت سرور: {response.StatusCode}"
                            });
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Error parsing API error response: {Content}", responseContent);
                        return Json(new
                        {
                            success = false,
                            message = $"❌ پاسخ نامعتبر از سمت سرور: {responseContent}"
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateValidationStatus for RequestId: {RequestId}", model.RequestId);
                return Json(new
                {
                    success = false,
                    message = $"خطا در برقراری ارتباط با سرور: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeRequest(long requestId)
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "توکن یافت نشد. لطفاً دوباره وارد سیستم شوید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = System.Text.Json.JsonSerializer.Serialize(new { RequestId = requestId });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("Request/Take", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse>(responseContent, options);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = apiResponse?.Message ?? "درخواست take شد." });
                }

                return Json(new { success = false, message = apiResponse?.Message ?? $"خطا از سمت سرور: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TakeRequest for RequestId: {RequestId}", requestId);
                return Json(new { success = false, message = $"خطا در برقراری ارتباط با سرور: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkDocumentAsRead(long requestId)
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "توکن یافت نشد. لطفاً دوباره وارد سیستم شوید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.PostAsync($"Service/MarkDocumentAsRead/{requestId}", null);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "سند با موفقیت به عنوان خوانده شده علامت‌گذاری شد." });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"خطا در علامت‌گذاری سند: {error}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentText(long requestId)
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "توکن یافت نشد. لطفاً دوباره وارد سیستم شوید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.GetAsync($"Service/GetTextByRequestId/{requestId}");

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<DocTextResponse>();
                    return Json(new { success = apiResponse.Success, documentText = apiResponse.DocumentText, isRead = apiResponse.IsRead });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"خطا در دریافت متن سند: {error}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در دریافت متن سند: {ex.Message}" });
            }
        }

        // فقط برای متصدی — متن کامل قبض انبار رو نشون می‌ده. متقاضی فقط شماره‌ی قبض رو
        // (که همراه بقیه‌ی اطلاعات درخواست تو جدول میاد) می‌بینه، نه این endpoint رو.
        [HttpGet]
        public async Task<IActionResult> GetWarehouseReceiptText(long requestId)
        {
            try
            {
                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "توکن یافت نشد. لطفاً دوباره وارد سیستم شوید." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.GetAsync($"Service/GetWarehouseReceiptByRequestId/{requestId}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = $"خطا در دریافت قبض انبار: {content}" });
                }

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا در دریافت قبض انبار: {ex.Message}" });
            }
        }

        [HttpGet]
        private async Task<PaginatedCartableViewModel> GetCartableData(
    int page,
    string search,
    string filterStatus,
    string? fromDate = null,
    string? toDate = null)
        {
            var pageSize = 10;
            var url =$"Request?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            if (!string.IsNullOrWhiteSpace(filterStatus))
            {
                url += $"&filterStatus={filterStatus}";
            }

            if (!string.IsNullOrWhiteSpace(fromDate))
            {
                url += $"&fromDate={Uri.EscapeDataString(fromDate)}";
            }

            if (!string.IsNullOrWhiteSpace(toDate))
            {
                url += $"&toDate={Uri.EscapeDataString(toDate)}";
            }

            try
            {
                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken as string;

                _logger.LogWarning($"TOKEN is null? {string.IsNullOrEmpty(token)}");
                _logger.LogWarning($"Request URL = {url}");

                if (!string.IsNullOrEmpty(token))
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _client.GetAsync(url);

                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogWarning("API StatusCode = {StatusCode}", response.StatusCode);
                _logger.LogWarning("API Response = {Response}", responseContent);


                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<PaginatedResponse<CartableItemViewModel>>();

                    // اضافه کردن این بخش برای تنظیم RejectReasonDisplay
                    if (data?.Items != null)
                    {
                        foreach (var item in data.Items)
                        {
                            if (item.ValidateByExpert == false && !string.IsNullOrEmpty(item.Description))
                            {
                                item.RejectReasonDisplay = item.Description;
                            }
                        }
                    }

                    return new PaginatedCartableViewModel
                    {
                        Items = data?.Items ?? new List<CartableItemViewModel>(),
                        TotalCount = data?.TotalCount ?? 0,
                        CurrentPage = page,
                        PageSize = pageSize,
                        SearchQuery = search
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cartable data for page {Page} with search {Search}", page, search);
            }
            return new PaginatedCartableViewModel { CurrentPage = page, PageSize = pageSize, SearchQuery = search };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateShahkar(CartableFormViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.NationalCode) || model.NationalCode.Length != 10 || !System.Text.RegularExpressions.Regex.IsMatch(model.NationalCode, @"^\d{10}$"))
                {
                    return Json(new { success = false, message = "کد ملی نامعتبر است." });
                }
                if (string.IsNullOrEmpty(model.MobileNumber) || model.MobileNumber.Length != 11 || !System.Text.RegularExpressions.Regex.IsMatch(model.MobileNumber, @"^09\d{9}$"))
                {
                    return Json(new { success = false, message = "شماره موبایل نامعتبر است." });
                }

                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید." });
                }
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.PostAsJsonAsync("Service/CheckMobileNationalCode", new
                {
                    NationalId = model.NationalCode,
                    MobileNumber = model.MobileNumber
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ShahkarResponse>();
                    bool isMatch = false;
                    try
                    {
                        var internalResponse = JsonSerializer.Deserialize<InternalShahkarResponse>(result.ResponseText, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        isMatch = internalResponse?.Result?.Data?.Response == 200;
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        return Json(new { success = false, message = "خطا در پردازش پاسخ سرویس شاهکار." });
                    }

                    if (isMatch)
                    {
                        return Json(new { success = true, message = "احراز هویت با موفقیت انجام شد." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "کد ملی و شماره موبایل تطابق ندارند." });
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"خطا در ارتباط با سرویس شاهکار: {error}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در احراز هویت با سرویس شاهکار برای کد ملی {NationalCode}", model.NationalCode);
                return Json(new { success = false, message = $"خطا در سرور: {ex.Message}" });
            }
        }

        #endregion

        [HttpGet]
        public IActionResult Shahkar()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VerifyDocument()
        {
            return View();
        }

    }

    #region Helper
    public static class PersianDateHelper
    {
        // برای DateTime (غیر nullable)
        public static string ToPersianDate(this DateTime date)
        {
            var pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(date)}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
        }

        public static string ToPersianTime(this DateTime date)
        {
            try
            {
                var iranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");

                var utcDate = date.Kind == DateTimeKind.Utc
                    ? date
                    : DateTime.SpecifyKind(date, DateTimeKind.Utc);

                var iranTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate, iranTimeZone);

                return iranTime.ToString("HH:mm:ss");
            }
            catch
            {
                return date.ToString("HH:mm:ss");
            }
        }

        // برای DateTime? (nullable)
        public static string ToPersianDate(this DateTime? date)
        {
            if (!date.HasValue) return "-";
            return date.Value.ToPersianDate();
        }

        public static string ToPersianTime(this DateTime? date)
        {
            if (!date.HasValue) return "-";
            return date.Value.ToPersianTime();
        }
    }
    #endregion

    #region ViewModels

    // اضافه کردن این مدل‌ها به CartableController.cs
    public class ApiResponseWrapper
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public CombinedResultData Data { get; set; }
    }

    public class CombinedResultData
    {
        public ShahkarResponse Shahkar { get; set; }
        public VerifyDocResponse VerifyDoc { get; set; }
        public long RequestId { get; set; }
    }

    public class CombinedRequestViewModel
    {
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public string? WarehouseReceiptNumber { get; set; }
    }

    public class VerifyDocResponse
    {
        public bool IsSuccessful { get; set; }
        public string ResponseText { get; set; }
        public List<PersonInQuery> PersonsInQuery { get; set; }
        public bool ExistDoc { get; set; }
        public bool IsNationalIdInLawyers { get; set; }
        public bool IsNationalIdInResponse { get; set; }
    }

    public class PersonInQuery
    {
        public string NationalNo { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string RoleType { get; set; }
        // سایر خصوصیات
    }
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class PaginatedCartableViewModel
    {
        public List<CartableItemViewModel> Items { get; set; } = new List<CartableItemViewModel>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string SearchQuery { get; set; } = string.Empty;
    }

    public class CartableFormViewModel
    {
        public string NationalCode { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string VerifyCode { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string ClientNationalCode { get; set; } = string.Empty;
        public string? WarehouseReceiptNumber { get; set; }
        public bool AgreeToTerms { get; set; }
        public string Step1Message { get; set; } = string.Empty;
        public string Step2Message { get; set; } = string.Empty;
        public string Step3Message { get; set; } = string.Empty;
    }

    public class CartableItemViewModel
    {
        public long RequestId { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public string? WarehouseReceiptNumber { get; set; }
        public string ImpotrtantAnnexText { get; set; } = string.Empty;
        public bool? IsMatch { get; set; }
        public bool? IsExist { get; set; }
        public bool? IsNationalIdInResponse { get; set; }
        public bool? IsNationalIdInLawyers { get; set; }
        public bool? ValidateByExpert { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsRead { get; set; } // اضافه شده برای وضعیت خوانده شدن سند
        public string RejectReasonDisplay { get; set; } = string.Empty;
        public int? GroupId { get; set; }
        public string? GroupTitle { get; set; }
        public long? AssignedTo { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? AssignedAt { get; set; }
    }


    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public class DocTextResponse
    {
        public bool Success { get; set; }
        public string? DocumentText { get; set; }
        public bool IsRead { get; set; }
        public string? Message { get; set; }
    }

    #endregion

}