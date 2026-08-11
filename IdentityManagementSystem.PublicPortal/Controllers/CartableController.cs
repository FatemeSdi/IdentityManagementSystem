using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using IdentityManagementSystem.PublicPortal.Filters;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace IdentityManagementSystem.PublicPortal.Controllers
{
    [NoCacheFilter]
    public class CartableController : Controller
    {
        private readonly HttpClient _client;
        private readonly ILogger<CartableController> _logger;
        private readonly IConfiguration _configuration;

        // کش ساده‌ی توکن حساب سرویسی «پورتال عمومی» — بین درخواست‌های anonymous مختلف به اشتراک می‌ره
        // تا هر ثبت درخواست مجبور به لاگین دوباره نباشه. Controller instance جدید ولی static field مشترکه.
        private static string? _publicPortalTokenCache;
        private static DateTime _publicPortalTokenExpiresAt;
        private static readonly SemaphoreSlim _publicPortalTokenLock = new(1, 1);

        public CartableController(
            IHttpClientFactory httpClientFactory,
            ILogger<CartableController> logger,
            IConfiguration configuration)
        {
            _client = httpClientFactory.CreateClient("PomixApi");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// توکن حساب سرویسی «پورتال عمومی» (PublicPortal تو appsettings) — برای متقاضی‌هایی که
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

        /// <summary>
        /// اگه trackMobile داده شده باشه، یعنی متقاضی داره درخواست‌های «در انتظار بررسی»ش رو با موبایل
        /// (بدون کد پیگیری) پیگیری می‌کنه. لیست اینجا، سمت کنترلر، از API گرفته و به View داده می‌شه —
        /// نه با AJAX/جاوااسکریپت. اگه هنوز OTP اون شماره تایید نشده باشه، فقط پرچمش به View می‌ره
        /// تا فرم OTP رو نشون بده؛ لیستی گرفته نمی‌شه.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ClientIndex(string? trackMobile)
        {
            ViewBag.FormModel = new CartableFormViewModel();

            if (!string.IsNullOrWhiteSpace(trackMobile))
            {
                trackMobile = trackMobile.Trim();
                ViewBag.TrackMobile = trackMobile;

                var otpVerified = HttpContext.Session.GetString($"OTP_Verified_{trackMobile}") == "true";
                ViewBag.TrackOtpVerified = otpVerified;

                if (otpVerified)
                {
                    ViewBag.PendingRequests = await GetPendingRequestsByMobileAsync(trackMobile);
                }
            }

            return View();
        }

        private async Task<List<PendingRequestItem>> GetPendingRequestsByMobileAsync(string mobileNumber)
        {
            try
            {
                var token = await GetPublicPortalTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new List<PendingRequestItem>();
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.GetAsync($"Request/TrackByMobile?mobileNumber={Uri.EscapeDataString(mobileNumber)}");
                if (!response.IsSuccessStatusCode)
                {
                    return new List<PendingRequestItem>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TrackByMobileResult>(content);
                return result?.Items ?? new List<PendingRequestItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت درخواست‌های در انتظار برای موبایل {MobileNumber}", mobileNumber);
                return new List<PendingRequestItem>();
            }
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
                var token = await GetPublicPortalTokenAsync();
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
        public IActionResult SendOtp(string? nationalCode, string mobileNumber)
        {
            // این endpoint بین فرم «ثبت درخواست» (که کد ملی داره) و فرم «پیگیری با موبایل» (که نداره)
            // مشترکه — فقط وقتی مقدار داده شده چک فرمتش می‌شه، نبودنش (مسیر پیگیری) اوکیه.
            if (!string.IsNullOrWhiteSpace(nationalCode) && nationalCode.Length != 10)
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

        /// <summary>
        /// پیگیری وضعیت درخواست با کد پیگیری + شماره موبایل — بدون نیاز به لاگین.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TrackRequest(string trackingCode, string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingCode) || string.IsNullOrWhiteSpace(mobileNumber))
            {
                return Json(new { success = false, message = "کد پیگیری و شماره موبایل الزامی است." });
            }

            try
            {
                var token = await GetPublicPortalTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "سامانه موقتاً در دسترس نیست." });
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _client.GetAsync($"Request/Track?trackingCode={Uri.EscapeDataString(trackingCode)}&mobileNumber={Uri.EscapeDataString(mobileNumber)}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    dynamic? errorResult = null;
                    try { errorResult = JsonConvert.DeserializeObject<dynamic>(content); } catch { }
                    string errorMessage = (string?)errorResult?.message ?? "درخواستی با این مشخصات یافت نشد.";
                    return Json(new { success = false, message = errorMessage });
                }

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در پیگیری درخواست {TrackingCode}", trackingCode);
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }

        // لیست درخواست‌های «در انتظار بررسی» با فقط موبایل (بدون کد پیگیری) دیگه از این کنترلر به‌صورت
        // AJAX سرو نمی‌شه — سمت کنترلر تو خودِ اکشن ClientIndex (پارامتر trackMobile) رندر می‌شه.
        // به GetPendingRequestsByMobileAsync بالا نگاه کن.

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

            // تایید OTP الزامیه — این چک سمت سرور انجام می‌شه (نه فقط JS) چون کسی می‌تونه
            // مستقیم این endpoint رو صدا بزنه.
            var otpVerified = HttpContext.Session.GetString($"OTP_Verified_{model.MobileNumber}") == "true";
            if (!otpVerified)
            {
                return Json(new { success = false, message = "لطفاً ابتدا شماره موبایل متقاضی را با کد پیامکی تایید کنید." });
            }

            var token = await GetPublicPortalTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "سامانه موقتاً در دسترس نیست. لطفاً کمی بعد دوباره تلاش کنید." });
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
                // متقاضی از بخش «پیگیری درخواست» می‌بینه، نه از این پیام.
                bool? isMatch = null;
                if (result?.data?.Shahkar != null)
                {
                    isMatch = (bool)(result.data.Shahkar.IsSuccessful == true);
                }

                string? trackingCode = (string?)result?.data?.TrackingCode;

                string message = !string.IsNullOrEmpty(trackingCode)
                    ? $"درخواست شما با کد پیگیری {trackingCode} ثبت شد."
                    : (isMatch == false
                        ? "درخواست شما ثبت شد. وضعیت آن را از بخش پیگیری درخواست دنبال کنید."
                        : "درخواست شما با موفقیت ثبت شد.");

                return Json(new { success = true, message, trackingCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ثبت درخواست جدید برای کد ملی {NationalCode}", model.NationalCode);
                return Json(new { success = false, message = $"خطا در ارتباط با سرور: {ex.Message}" });
            }
        }
    }

    public class CartableFormViewModel
    {
        public string NationalCode { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string VerifyCode { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string? WarehouseReceiptNumber { get; set; }
    }

    public class CombinedRequestViewModel
    {
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public string? WarehouseReceiptNumber { get; set; }
    }

    public class LoginResponse
    {
        public TokenInfo? Tokens { get; set; }
    }

    public class TrackByMobileResult
    {
        public bool Success { get; set; }
        public List<PendingRequestItem>? Items { get; set; }
    }

    public class PendingRequestItem
    {
        public string? TrackingCode { get; set; }
        public string? WarehouseReceiptNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TokenInfo
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
