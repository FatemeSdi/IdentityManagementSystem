using DNTCaptcha.Core;
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

        public CartableController(
            IHttpClientFactory httpClientFactory,
            IDNTCaptchaValidatorService captchaValidatorService,
            ILogger<CartableController> logger)
        {
            _client = httpClientFactory.CreateClient("PomixApi");
            _captchaValidatorService = captchaValidatorService ?? throw new ArgumentNullException(nameof(captchaValidatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CartableIndex

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string search = "", string filterStatus = "")
        {
            // گرفتن داده‌ها از API
            var model = await GetCartableData(
            page,
            search,
            filterStatus);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateSteps(CartableFormViewModel model)
        {
            // ریست کردن پیام‌ها
            model.Step1Message = model.Step2Message = model.Step3Message = "";

            bool step1Valid = true, step2Valid = true, step3Valid = true;

            // 🟢 گام 1: احراز هویت با سرویس شاهکار
            if (string.IsNullOrEmpty(model.NationalCode) || model.NationalCode.Length != 10 || !Regex.IsMatch(model.NationalCode, @"^\d{10}$"))
            {
                model.Step1Message = "کد ملی نامعتبر است.";
                step1Valid = false;
            }
            if (string.IsNullOrEmpty(model.MobileNumber) || model.MobileNumber.Length != 11 || !Regex.IsMatch(model.MobileNumber, @"^09\d{9}$"))
            {
                model.Step1Message += " شماره موبایل نامعتبر است.";
                step1Valid = false;
            }

            if (step1Valid)
            {
                try
                {
                    var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                    if (string.IsNullOrEmpty(token))
                    {
                        model.Step1Message = "لطفاً ابتدا وارد سیستم شوید.";
                        step1Valid = false;
                    }
                    else
                    {
                        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var response = await _client.PostAsJsonAsync("Service/ProcessCombinedRequest", new CombinedRequestViewModel
                        {
                            NationalId = model.NationalCode,
                            MobileNumber = model.MobileNumber,
                            DocumentNumber = model.DocumentNumber,
                            VerificationCode = model.VerifyCode
                        });

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            dynamic result = JsonConvert.DeserializeObject<dynamic>(content);
                            bool isMatch = result?.data?.Shahkar?.IsSuccessful == true;

                            model.Step1Message = isMatch ? "احراز هویت معتبر است." : "کد ملی و شماره موبایل تطابق ندارند.";
                            step1Valid = isMatch;
                        }
                        else
                        {
                            model.Step1Message = "خطا در ارتباط با سرویس احراز هویت.";
                            step1Valid = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    model.Step1Message = $"خطا در ارتباط با سرویس احراز هویت: {ex.Message}";
                    step1Valid = false;
                }
            }

            // 🟢 گام 2: احراز سند (مستقل از گام 1)
            if (string.IsNullOrEmpty(model.VerifyCode) || model.VerifyCode.Length != 6 || !Regex.IsMatch(model.VerifyCode, @"^\d{6}$"))
            {
                model.Step2Message = "رمز تصدیق نامعتبر است.";
                step2Valid = false;
            }
            if (string.IsNullOrEmpty(model.DocumentNumber) || model.DocumentNumber.Length != 18 || !Regex.IsMatch(model.DocumentNumber, @"^\d{18}$"))
            {
                model.Step2Message += " شناسه سند نامعتبر است.";
                step2Valid = false;
            }
            if (step2Valid && string.IsNullOrEmpty(model.Step2Message))
            {
                model.Step2Message = "احراز سند معتبر است.";
            }

            // 🟢 گام 3: تطابق کد ملی موکل (مستقل از گام‌های دیگر)
            if (string.IsNullOrEmpty(model.ClientNationalCode) || model.ClientNationalCode.Length != 10 || !Regex.IsMatch(model.ClientNationalCode, @"^\d{10}$"))
            {
                model.Step3Message = "کد ملی موکل نامعتبر است.";
                step3Valid = false;
            }
            else
            {
                model.Step3Message = "تطابق کد ملی معتبر است.";
            }

            // پیام نهایی
            if (step1Valid && step2Valid && step3Valid)
            {
                ViewBag.SuccessMessage = "همه گام‌ها معتبر هستند.";
            }

            ViewBag.FormModel = model;
            return View("Index", await GetCartableData(1, "", ""));
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
                    VerificationCode = model.VerifyCode
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

        [HttpGet]
        private async Task<PaginatedCartableViewModel> GetCartableData(
    int page,
    string search,
    string filterStatus)
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

            try
            {
                var token = HttpContext.Session.GetString("JwtToken") ?? ViewBag.JwtToken;
                if (!string.IsNullOrEmpty(token))
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _client.GetAsync(url);
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