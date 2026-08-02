using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Request = IdentityManagementSystem.API.Models.Request;

namespace IdentityManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IdentityManagementSystemContext _context;
        private readonly ShahkarServiceOptions _options;
        private readonly BsrServiceOptions _bsrOptions;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ServiceController> _logger;
        private readonly string _providerCode = "0785";
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public ServiceController(
            HttpClient httpClient,
            IdentityManagementSystemContext context,
            IOptions<ShahkarServiceOptions> options,
            IOptions<BsrServiceOptions> bsrOptions,
            IMemoryCache cache,
            ILogger<ServiceController> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _bsrOptions = bsrOptions?.Value ?? new BsrServiceOptions();
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(_options.BaseUrl) || _options.Credential == null ||
                string.IsNullOrEmpty(_options.Credential.Code) || string.IsNullOrEmpty(_options.Credential.Password))
            {
                _logger.LogError("Shahkar service configuration is incomplete: BaseUrl={BaseUrl}, Code={Code}, Password={Password}",
                    _options.BaseUrl, _options.Credential?.Code, _options.Credential?.Password);
                throw new InvalidOperationException("تنظیمات سرویس شاهکار ناقص است.");
            }

            if (string.IsNullOrEmpty(_bsrOptions.Username) || string.IsNullOrEmpty(_bsrOptions.Password))
            {
                // بر خلاف شاهکار، نبود تنظیمات Bsr نباید کل کنترلر رو از کار بندازه —
                // فقط گام قبض انبار (که اختیاریه) موقع فراخوانی با خطای واضح fail می‌شه.
                _logger.LogWarning("Bsr service configuration is incomplete (Username/Password). Warehouse receipt lookups will fail until configured.");
            }

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} after {TimeSpan} seconds due to {Reason}",
                            retryCount, timeSpan.TotalSeconds, result.Exception?.Message ?? $"StatusCode: {result.Result?.StatusCode}");
                    });
        }

        [HttpPost("ProcessCombinedRequest")]
        [Authorize(Policy = "CanAccessServices")]
        public async Task<IActionResult> ProcessCombinedRequest([FromBody] CombinedRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return new JsonResult(new { success = false, message = "داده‌های ورودی نامعتبر است." });
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
            {
                _logger.LogWarning("Could not extract UserId from JWT token.");
                return new JsonResult(new { success = false, message = "کاربر شناسایی نشد." });
            }

            var requestCode = GenerateRequestId();
            var request = new Request
            {
                RequestCode = requestCode,
                NationalId = model.NationalId,
                MobileNumber = model.MobileNumber,
                DocumentNumber = model.DocumentNumber,
                VerificationCode = model.VerificationCode,
                WarehouseReceiptNumber = model.WarehouseReceiptNumber,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "Unknown"
            };

            _context.Request.Add(request);
            await _context.SaveChangesAsync();

            // === ثبت تاریخچه درخواست (RequestHistory) ===
            var history = new RequestHistory
            {
                RequestId = request.RequestId,
                StatusId = 1,                                          // 1 = در انتظار بررسی
                ExpertId = userId.ToString(),                          // string مطابق اسکیمای DB (nvarchar)
                ActionDescription = "درخواست جدید از فرم کارتابل ایجاد شد.",
                CreatedAt = DateTime.UtcNow,
                UpdatedStatus = "در انتظار بررسی",
                UpdatedStatusBy = User.Identity?.Name ?? "سیستم",
                UpdatedStatusDate = DateTime.UtcNow
            };

            _context.RequestHistory.Add(history);
            await _context.SaveChangesAsync();

            long expertId = userId;

            // === قبض انبار — مستقل از گام‌های احراز هویت/سند، فقط اگه شماره‌ش وارد شده باشه.
            // عمداً همینجا (قبل از Shahkar/VerifyDoc) قرار گرفته که به هیچ‌کدوم از return‌های
            // early اون دو گام وابسته نباشه و همیشه دقیقاً یک‌بار اجرا بشه.
            if (!string.IsNullOrWhiteSpace(model.WarehouseReceiptNumber))
            {
                await ProcessWarehouseReceipt_Internal(model.WarehouseReceiptNumber, request.RequestId, userId);
            }

            // === گام ۱: احراز هویت (شاهکار) — تا این تایید نشه سراغ گام بعد نمی‌ریم ===
            ShahkarResponse shahkarResult;
            try
            {
                shahkarResult = await CheckMobileNationalCode_Internal(
                    model.NationalId, model.MobileNumber, request.RequestCode, request.RequestId, expertId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shahkar check failed for {RequestId}", request.RequestId);
                await FinalizeRequestAsync(request, isMatch: false, description: $"خطا در احراز هویت: {ex.Message}");
                return new JsonResult(new
                {
                    success = true,
                    data = new { Shahkar = (ShahkarResponse?)null, VerifyDoc = (VerifyDocResponse?)null, RequestId = request.RequestId },
                    message = "درخواست ثبت شد؛ اما احراز هویت با خطا مواجه شد."
                });
            }

            bool isMatch = false;
            try
            {
                var internalShahkarResponse = JsonSerializer.Deserialize<InternalShahkarResponse>(
                    shahkarResult.ResponseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                isMatch = internalShahkarResponse?.Result?.Data?.Response == 200;
                _logger.LogInformation("Shahkar IsMatch for {RequestId}: {IsMatch}", request.RequestId, isMatch);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse internal Shahkar response: {ResponseText}", shahkarResult.ResponseText);
            }

            if (!isMatch)
            {
                // احراز هویت رد شد → درخواست همینجا ثبت و متوقف می‌شه، سراغ بررسی سند نمی‌ریم
                await FinalizeRequestAsync(request, isMatch: false, description: null);
                return new JsonResult(new
                {
                    success = true,
                    data = new { Shahkar = shahkarResult, VerifyDoc = (VerifyDocResponse?)null, RequestId = request.RequestId }
                });
            }

            // === گام ۲: بررسی سند (وجود سند، تطابق کدملی، وکالت) — فقط اگه گام ۱ تایید شده باشه ===
            VerifyDocResponse verifyDocResult;
            try
            {
                verifyDocResult = await VerifyDocument_Internal(
                    model.DocumentNumber, model.VerificationCode, request.RequestCode, request.RequestId, userId, model.NationalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyDocument failed for {RequestId}", request.RequestId);
                await FinalizeRequestAsync(request, isMatch: true, description: $"احراز هویت موفق بود؛ خطا در بررسی سند: {ex.Message}");
                return new JsonResult(new
                {
                    success = true,
                    data = new { Shahkar = shahkarResult, VerifyDoc = (VerifyDocResponse?)null, RequestId = request.RequestId },
                    message = "احراز هویت موفق بود؛ اما بررسی سند با خطا مواجه شد."
                });
            }

            request.IsExist = verifyDocResult.ExistDoc;
            request.IsNationalIdInResponse = verifyDocResult.IsNationalIdInResponse;
            request.IsNationalIdInLawyers = verifyDocResult.IsNationalIdInLawyers;
            await FinalizeRequestAsync(request, isMatch: true, description: null);

            var combinedResult = new
            {
                Shahkar = shahkarResult,
                VerifyDoc = verifyDocResult,
                RequestId = request.RequestId
            };

            return new JsonResult(new { success = true, data = combinedResult });
        }

        /// <summary>
        /// درخواست رو با نتیجه‌ی نهایی (هر مرحله‌ای که تا اینجا رسیده) در دیتابیس finalize می‌کنه.
        /// طوری طراحی شده که هیچوقت throw نکنه تا رکورد درخواست یتیم/بدون به‌روزرسانی نمونه.
        /// </summary>
        private async Task FinalizeRequestAsync(Request request, bool isMatch, string? description)
        {
            try
            {
                request.IsMatch = isMatch;
                if (description != null)
                {
                    request.Description = description;
                }
                request.UpdatedAt = DateTime.UtcNow;
                request.UpdatedBy = User.Identity?.Name ?? "Unknown";
                _context.Request.Update(request);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to finalize request {RequestId}", request.RequestId);
            }
        }

        private async Task<ShahkarResponse> CheckMobileNationalCode_Internal(
            string nationalId, string mobile, string requestCode, long requestId, long expertId)
        {
            if (nationalId.Length != 10 || !nationalId.All(char.IsDigit))
            {
                _logger.LogWarning("Invalid NationalId format: NationalId={NationalId}", nationalId);
                throw new ArgumentException("کد ملی باید 10 رقم باشد.");
            }
            if (mobile.Length != 11 || !mobile.All(char.IsDigit) || !mobile.StartsWith("09"))
            {
                _logger.LogWarning("Invalid MobileNumber format: MobileNumber={MobileNumber}", mobile);
                throw new ArgumentException("شماره موبایل باید 11 رقم باشد و با 09 شروع شود.");
            }
            if (!IsValidIranianNationalId(nationalId))
            {
                _logger.LogWarning("Invalid NationalId: {NationalId}", nationalId);
                throw new ArgumentException("کد ملی نامعتبر است.");
            }
            if (!await CanMakeShahkarRequest(expertId))
            {
                _logger.LogWarning("Request limit exceeded for expert {ExpertId}", expertId);
                throw new InvalidOperationException("تعداد درخواست‌های شما از حد مجاز گذشته است.");
            }

            string cacheKey = $"Shahkar_{nationalId}_{mobile}";
            if (_cache.TryGetValue(cacheKey, out ShahkarResponse? cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("Returning cached result for {CacheKey}", cacheKey);
                return cachedResult;
            }

            try
            {
                var requestContent = new
                {
                    credential = new
                    {
                        code = _options.Credential.Code,
                        password = _options.Credential.Password
                    },
                    parameters = new[]
                    {
                        new { parameterName = "serviceType", parameterValue = "2" },
                        new { parameterName = "identificationType", parameterValue = "0" },
                        new { parameterName = "identificationNo", parameterValue = nationalId },
                        new { parameterName = "requestId", parameterValue = requestCode },
                        new { parameterName = "serviceNumber", parameterValue = mobile }
                    },
                    service = "gsb-itoshahkar"
                };

                var content = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json");
                _logger.LogInformation("Sending request to Shahkar: URL={Url}, RequestContent={RequestContent}, Headers={Headers}",
                    _options.BaseUrl, await content.ReadAsStringAsync(), _httpClient.DefaultRequestHeaders);

                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(_options.BaseUrl, content));
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Received response from Shahkar: StatusCode={StatusCode}, Content={ResponseContent}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Shahkar request failed: StatusCode={StatusCode}, Response={ResponseContent}",
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"خطای سرویس شاهکار: کد {(int)response.StatusCode}");
                }

                ShahkarResponse? result;
                try
                {
                    result = JsonSerializer.Deserialize<ShahkarResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize Shahkar response for {RequestId}: ResponseContent={ResponseContent}",
                        requestCode, responseContent);
                    throw new Exception($"خطا در پردازش پاسخ سرویس شاهکار: {ex.Message}");
                }

                if (result == null || string.IsNullOrEmpty(result.ResponseText))
                {
                    _logger.LogWarning("Shahkar response is null or empty for {RequestId}", requestCode);
                    throw new Exception("پاسخ سرویس شاهکار خالی است.");
                }

                bool isMatch = false;
                try
                {
                    var internalResponse = JsonSerializer.Deserialize<InternalShahkarResponse>(result.ResponseText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (internalResponse?.Result?.Data?.Response == 200)
                    {
                        isMatch = true;
                    }
                    else if (internalResponse?.Result?.Data?.Response == 600)
                    {
                        isMatch = false;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid response code from Shahkar: {ResponseCode}, Comment: {Comment}",
                            internalResponse?.Result?.Data?.Response, internalResponse?.Result?.Data?.Comment);
                        throw new Exception($"خطای نامشخص از سرویس شاهکار: {internalResponse?.Result?.Data?.Comment}");
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse internal Shahkar response: {ResponseText}", result.ResponseText);
                    throw new Exception($"خطا در پردازش پاسخ داخلی سرویس شاهکار: {ex.Message}");
                }

                var log = new ShahkarLog
                {
                    NationalId = nationalId,
                    MobileNumber = mobile,
                    RequestCode = requestCode,
                    IsMatch = isMatch,
                    ResponseText = responseContent,
                    ExpertId = expertId,
                    CreatedAt = DateTime.UtcNow,
                    RequestId = requestId
                };
                _context.ShahkarLog.Add(log);
                await _context.SaveChangesAsync();

                result.ResponseStatusCode = (int)response.StatusCode;
                result.RequestId = requestId.ToString();

                try
                {
                    _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
                    _logger.LogInformation("Cached result for {CacheKey}", cacheKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache result for {CacheKey}", cacheKey);
                }

                _logger.LogInformation("Mobile verification successful for {NationalId}", nationalId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for {NationalId}", nationalId);
                throw new Exception($"خطای ارتباط با سرویس شاهکار: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during mobile verification for {NationalId}", nationalId);
                throw new Exception($"خطای سرور: {ex.Message}");
            }
        }

        private async Task<VerifyDocResponse> VerifyDocument_Internal(string documentNumber, string verificationCode, string requestCode, long requestId, long userId, string nationalId)
        {
            if (documentNumber.Length != 18 || !documentNumber.All(char.IsDigit))
                throw new ArgumentException("DocumentNumber نامعتبر است.");
            if (verificationCode.Length != 6 || !verificationCode.All(char.IsDigit))
                throw new ArgumentException("VerificationCode نامعتبر است.");

            var requestBody = new
            {
                credential = new { code = _options.Credential.Code, password = _options.Credential.Password },
                parameters = new object[]
                {
                    new { parameterName = "NationalRegisterNo", parameterValue = documentNumber },
                    new { parameterName = "SecretNo", parameterValue = verificationCode },
                    new { parameterName = "requestId", parameterValue = requestCode }
                },
                service = "gsb-Approval2-GetData"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogInformation("Sending request to VerifyDoc: URL={Url}, RequestContent={RequestContent}",
                _options.BaseUrl, json);

            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(_options.BaseUrl, content));
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received response from VerifyDoc: StatusCode={StatusCode}, Content={ResponseContent}",
                response.StatusCode, responseString);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VerifyDoc request failed: StatusCode={StatusCode}, Response={ResponseContent}",
                    response.StatusCode, responseString);
                throw new HttpRequestException($"خطای سرویس اصالت سند: کد {(int)response.StatusCode}");
            }

            VerifyDocResponse result = new VerifyDocResponse
            {
                IsSuccessful = false,
                ResponseText = responseString,
                PersonsInQuery = new List<PersonInQuery>(),
                ExistDoc = false,
                IsNationalIdInLawyers = false,
                IsNationalIdInResponse = false
            };

            bool isExist = false;
            bool succseed = false;
            List<PersonInQuery> personsInQuery = new List<PersonInQuery>();

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("responseText", out var responseTextElement) && responseTextElement.ValueKind == JsonValueKind.String)
                {
                    try
                    {
                        using var responseTextDoc = JsonDocument.Parse(responseTextElement.GetString());
                        if (responseTextDoc.RootElement.TryGetProperty("result", out var resultElement) &&
                            resultElement.TryGetProperty("data", out var dataElement))
                        {
                            isExist = dataElement.TryGetProperty("ExistDoc", out var existDocElement) && existDocElement.GetBoolean();
                            succseed = dataElement.TryGetProperty("succseed", out var succseedElement) && succseedElement.GetBoolean();

                            if (dataElement.TryGetProperty("lstFindPersonInQuery", out var personsElement) && personsElement.ValueKind == JsonValueKind.Array)
                            {
                                personsInQuery = JsonSerializer.Deserialize<List<PersonInQuery>>(personsElement.GetRawText(), new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                }) ?? new List<PersonInQuery>();

                                bool isNationalIdInResponse = personsInQuery.Any(p => EqualsNormalized(p.NationalNo, nationalId));
                                var lawyers = personsInQuery
                                    .Where(p => !string.IsNullOrWhiteSpace(p.RoleType) && EqualsNormalized(p.RoleType, "وکیل"))
                                    .ToList();
                                bool isApplicantLawyer = lawyers.Any(p => EqualsNormalized(p.NationalNo, nationalId));

                                _logger.LogInformation("Applicant NationalId exists in response: {IsNationalIdInResponse}, among lawyers: {IsApplicantLawyer}",
                                    isNationalIdInResponse, isApplicantLawyer);

                                result.IsNationalIdInResponse = isNationalIdInResponse;
                                result.IsNationalIdInLawyers = isApplicantLawyer;
                            }

                            _logger.LogInformation("Parsed VerifyDoc data - isExist: {IsExist}, succseed: {Succseed}, personsInQuery count: {PersonsInQueryCount}",
                                isExist, succseed, personsInQuery.Count);
                        }
                        else
                        {
                            _logger.LogWarning("Missing 'result' or 'data' in responseText for {RequestId}", requestCode);
                        }
                    }
                    catch (JsonException nestedEx)
                    {
                        _logger.LogError(nestedEx, "Failed to parse nested responseText for {RequestId}: {ResponseText}", requestCode, responseTextElement.GetString());
                    }
                }
                else
                {
                    _logger.LogWarning("Missing or invalid 'responseText' property for {RequestId}", requestCode);
                }

                result.IsSuccessful = isExist || succseed;
                result.PersonsInQuery = personsInQuery;
                result.ExistDoc = isExist;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize VerifyDoc response for {RequestId}: ResponseContent={ResponseContent}",
                    requestCode, responseString);
                result = new VerifyDocResponse
                {
                    IsSuccessful = false,
                    ResponseText = responseString,
                    PersonsInQuery = new List<PersonInQuery>(),
                    ExistDoc = false,
                    IsNationalIdInResponse = false,
                    IsNationalIdInLawyers = false
                };
            }

            var log = new VerifyDocLog
            {
                DocumentNumber = documentNumber,
                VerificationCode = verificationCode,
                ResponseText = responseString,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? userId.ToString(),
                IsExist = isExist,
                RequestId = requestId
            };

            _logger.LogInformation("Saving VerifyDocLog - DocumentNumber: {DocumentNumber}, IsExist: {IsExist}, RequestId: {RequestId}",
                log.DocumentNumber, log.IsExist, log.RequestId);
            _context.VerifyDocLog.Add(log);
            await _context.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// دریافت قبض انبار (bsr-GetPortIncomeInvoice) و ثبت جزئیات استخراج‌شده در Define.WarehouseReceipt
        /// + متن خام پاسخ در Log.WarehouseReceiptLog. طوری نوشته شده که هیچوقت throw نکنه — شکست این گام
        /// (که مستقل و اختیاریه) نباید مانع ادامه‌ی پردازش Shahkar/VerifyDoc بشه.
        /// </summary>
        private async Task ProcessWarehouseReceipt_Internal(string receiptNumber, long requestId, long userId)
        {
            var createdBy = User.Identity?.Name ?? userId.ToString();
            var receiptParams = new[] { ("WarehouseReceiptNumber", receiptNumber) };

            // === فاکتور درآمد بندری — همونی که Define.WarehouseReceipt رو پر می‌کنه ===
            var portIncomeResult = await CallBsrServiceAsync("bsr-GetPortIncomeInvoice", receiptParams);
            await LogWarehouseReceiptCallAsync(requestId, receiptNumber, "PortIncomeInvoice", portIncomeResult, createdBy);

            var items = new List<PortIncomeInvoiceItem>();
            if (!string.IsNullOrWhiteSpace(portIncomeResult.InnerResponseText))
            {
                try
                {
                    items = JsonSerializer.Deserialize<List<PortIncomeInvoiceItem>>(portIncomeResult.InnerResponseText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<PortIncomeInvoiceItem>();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "خطا در پردازش پاسخ GetPortIncomeInvoice برای درخواست {RequestId}", requestId);
                }
            }

            try
            {
                if (items.Count > 0)
                {
                    // یک ردیف به‌ازای هر قلم فاکتور (چون یک قبض انبار می‌تونه چند فاکتور/تخلیه داشته باشه)
                    foreach (var item in items)
                    {
                        _context.WarehouseReceipts.Add(new WarehouseReceipt
                        {
                            RequestId = requestId,
                            ReceiptNumber = string.IsNullOrWhiteSpace(item.ReceiptNumber) ? receiptNumber : item.ReceiptNumber,
                            SerialNumber = item.InvoiceNumber,
                            OwnerNationalId = item.GoodsOwnerNationalID,
                            Quantity = item.Weight,
                            Unit = item.Weight.HasValue ? "kg" : null,
                            IssueDate = ParsePersianDateSafe(item.DischargeDate) ?? ParsePersianDateSafe(item.InvoiceDate),
                            IsVerified = portIncomeResult.IsSuccessful,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = createdBy
                        });
                    }
                }
                else
                {
                    // حتی اگه هیچ فاکتوری برنگشت، یه ردیف با نتیجه‌ی «تایید نشده» ثبت می‌کنیم
                    // تا مشخص باشه این شماره قبض بررسی شده ولی چیزی پیدا نشده.
                    _context.WarehouseReceipts.Add(new WarehouseReceipt
                    {
                        RequestId = requestId,
                        ReceiptNumber = receiptNumber,
                        IsVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = createdBy
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "ثبت اطلاعات قبض انبار برای درخواست {RequestId} در دیتابیس ناموفق بود", requestId);
            }

            // === هزینه خدمات / بیمه / پارکینگ — فقط لاگ خام ثبت می‌شه.
            // چون نمونه‌ی واقعی پاسخ این سه سرویس در دسترس نبود، استخراج فیلد به فیلد (مثل PortIncomeInvoice)
            // فعلاً انجام نمی‌شه تا از حدس اشتباه پرهیز بشه؛ متن خام برای استفاده‌ی بعدی ذخیره‌ست. ===
            var serviceCostResult = await CallBsrServiceAsync("bsr-GetServiceCostInvoic", receiptParams);
            await LogWarehouseReceiptCallAsync(requestId, receiptNumber, "ServiceCostInvoice", serviceCostResult, createdBy);

            var insuranceResult = await CallBsrServiceAsync("bsr-GetInsuranceInvoice", receiptParams);
            await LogWarehouseReceiptCallAsync(requestId, receiptNumber, "InsuranceInvoice", insuranceResult, createdBy);

            var parkingCostResult = await CallBsrServiceAsync("bsr-GetParkingCostInvoic", receiptParams);
            await LogWarehouseReceiptCallAsync(requestId, receiptNumber, "ParkingCostInvoice", parkingCostResult, createdBy);
        }

        private async Task LogWarehouseReceiptCallAsync(long requestId, string receiptNumber, string serviceType, BsrServiceCallResult result, string createdBy)
        {
            try
            {
                _context.WarehouseReceiptLogs.Add(new WarehouseReceiptLog
                {
                    RequestId = requestId,
                    ReceiptNumber = receiptNumber,
                    ServiceType = serviceType,
                    ResponseText = result.RawResponseText,
                    IsSuccessful = result.IsSuccessful,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ثبت لاگ سرویس {ServiceType} برای درخواست {RequestId} ناموفق بود", serviceType, requestId);
            }
        }

        /// <summary>
        /// متد عمومی برای فراخوانی هر سرویس bsr-* (توکن‌دار). گرفتن توکن، ست کردن هدر Authorization
        /// فقط روی همین یک درخواست (نه _httpClient.DefaultRequestHeaders — که تماس‌های Shahkar/VerifyDoc
        /// دست‌نخورده بمونن)، retry، و لاگ کردن پاسخ خام رو یکجا انجام می‌ده. هیچوقت throw نمی‌کنه؛
        /// خطا در IsSuccessful=false و ErrorMessage نتیجه منعکس می‌شه.
        /// </summary>
        private async Task<BsrServiceCallResult> CallBsrServiceAsync(string service, IEnumerable<(string Name, string Value)> parameters)
        {
            var result = new BsrServiceCallResult();
            try
            {
                var token = await GetBsrAuthTokenAsync();

                var requestBody = new
                {
                    credential = new { code = _options.Credential.Code, password = _options.Credential.Password },
                    parameters = parameters.Select(p => new { parameterName = p.Name, parameterValue = p.Value }).ToArray(),
                    service
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.TryAddWithoutValidation("Authorization", token);

                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(httpRequest));
                result.RawResponseText = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Received response from {Service}: StatusCode={StatusCode}, Content={ResponseContent}",
                    service, response.StatusCode, result.RawResponseText);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"خطای سرویس {service}: کد {(int)response.StatusCode}");
                }

                using var doc = JsonDocument.Parse(result.RawResponseText);
                var root = doc.RootElement;
                result.IsSuccessful = root.TryGetProperty("isSuccessful", out var isSuccessfulEl) &&
                                       isSuccessfulEl.ValueKind == JsonValueKind.True;

                if (root.TryGetProperty("responseText", out var responseTextEl) && responseTextEl.ValueKind == JsonValueKind.String)
                {
                    result.InnerResponseText = responseTextEl.GetString();
                }

                if (!result.IsSuccessful && root.TryGetProperty("errorDescription", out var errDescEl) && errDescEl.ValueKind == JsonValueKind.String)
                {
                    result.ErrorMessage = errDescEl.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در فراخوانی سرویس {Service}", service);
                result.ErrorMessage ??= ex.Message;
            }

            return result;
        }

        /// <summary>
        /// bsr-GetPortIncomeByDate — گزارش درآمد بندری در یک بازه‌ی تاریخی (نه مرتبط با یک درخواست خاص).
        /// فعلاً به هیچ endpoint/UI‌یی وصل نیست؛ آماده‌ست تا هروقت محل نمایشش (مثلاً صفحه‌ی گزارش‌ها) مشخص شد استفاده بشه.
        /// </summary>
        private async Task<BsrServiceCallResult> GetPortIncomeByDate(string startDate, string endDate, long lastReceivedInvoiceId = 0, int countRowsPerRequest = 50)
        {
            return await CallBsrServiceAsync("bsr-GetPortIncomeByDate", new[]
            {
                ("StartDate", startDate),
                ("EndDate", endDate),
                ("LastRecivedInvoiceID", lastReceivedInvoiceId.ToString()),
                ("CountRowsPerRequest", countRowsPerRequest.ToString())
            });
        }

        /// <summary>
        /// bsr-GetExitGateByInvDate — گزارش خروج از دروازه بر اساس تاریخ فاکتور (نه مرتبط با یک درخواست خاص).
        /// فعلاً به هیچ endpoint/UI‌یی وصل نیست؛ آماده‌ست تا هروقت محل نمایشش مشخص شد استفاده بشه.
        /// </summary>
        private async Task<BsrServiceCallResult> GetExitGateByInvDate(string startDate, string endDate, long lastReceivedInvoiceId = 0, int countRowsPerRequest = 50)
        {
            return await CallBsrServiceAsync("bsr-GetExitGateByInvDate", new[]
            {
                ("StartDate", startDate),
                ("EndDate", endDate),
                ("LastRecivedInvoiceID", lastReceivedInvoiceId.ToString()),
                ("CountRowsPerRequest", countRowsPerRequest.ToString())
            });
        }

        /// <summary>
        /// گرفتن توکن Bearer سرویس‌های bsr-* از طریق bsr-login و کش کردنش —
        /// طوری که هر تماس (تایید/رد/قبض انبار) نیازی به لاگین دوباره نداشته باشه.
        /// </summary>
        private async Task<string> GetBsrAuthTokenAsync()
        {
            const string cacheKey = "Bsr_AuthToken";
            if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
            {
                return cachedToken;
            }

            if (string.IsNullOrEmpty(_bsrOptions.Username) || string.IsNullOrEmpty(_bsrOptions.Password))
            {
                throw new InvalidOperationException("تنظیمات ورود سرویس قبض انبار (Bsr:Username/Password) ناقص است.");
            }

            var loginBody = new
            {
                credential = new { code = _options.Credential.Code, password = _options.Credential.Password },
                parameters = new[]
                {
                    new { parameterName = "Username", parameterValue = _bsrOptions.Username },
                    new { parameterName = "Password", parameterValue = _bsrOptions.Password }
                },
                service = "bsr-login"
            };

            var content = new StringContent(JsonSerializer.Serialize(loginBody), Encoding.UTF8, "application/json");
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(_options.BaseUrl, content));
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Received response from bsr-login: StatusCode={StatusCode}, Content={ResponseContent}",
                response.StatusCode, responseText);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"خطای ورود به سرویس قبض انبار: کد {(int)response.StatusCode}");
            }

            var token = ExtractBsrToken(responseText);
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("توکن سرویس قبض انبار در پاسخ bsr-login یافت نشد.");
            }

            // مدت اعتبار واقعی توکن مشخص نیست؛ برای احتیاط کوتاه کش می‌کنیم و در صورت خطای بعدی دوباره لاگین می‌کنیم.
            _cache.Set(cacheKey, token, TimeSpan.FromMinutes(10));
            return token;
        }

        /// <summary>
        /// استخراج توکن از پاسخ bsr-login. چون نمونه‌ی واقعی پاسخ این سرویس در دسترس نبود،
        /// چند شکل محتمل رو امتحان می‌کنه و خام پاسخ رو (بالا، در GetBsrAuthTokenAsync) لاگ می‌کنه
        /// تا در صورت اشتباه بودن، به‌راحتی از لاگ قابل تشخیص و اصلاح باشه.
        /// </summary>
        private string? ExtractBsrToken(string rawResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawResponse);
                var root = doc.RootElement;

                string[] tokenFieldNames = { "token", "Token", "accessToken", "AccessToken", "authToken", "AuthToken" };

                foreach (var name in tokenFieldNames)
                {
                    if (root.TryGetProperty(name, out var tokenEl) && tokenEl.ValueKind == JsonValueKind.String)
                        return tokenEl.GetString();
                }

                if (root.TryGetProperty("responseText", out var rt) && rt.ValueKind == JsonValueKind.String)
                {
                    var text = rt.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                        return null;

                    if (text.TrimStart().StartsWith("{"))
                    {
                        using var inner = JsonDocument.Parse(text);
                        var innerRoot = inner.RootElement;

                        foreach (var name in tokenFieldNames)
                        {
                            if (innerRoot.TryGetProperty(name, out var tokenEl) && tokenEl.ValueKind == JsonValueKind.String)
                                return tokenEl.GetString();
                        }

                        if (innerRoot.TryGetProperty("result", out var resultEl) &&
                            resultEl.TryGetProperty("data", out var dataEl))
                        {
                            foreach (var name in tokenFieldNames)
                            {
                                if (dataEl.TryGetProperty(name, out var tokenEl) && tokenEl.ValueKind == JsonValueKind.String)
                                    return tokenEl.GetString();
                            }
                        }

                        return null;
                    }

                    // اگه JSON نبود، احتمالاً خود متن، توکن خامه
                    return text;
                }
            }
            catch (JsonException)
            {
                // اگه کل پاسخ اصلاً JSON نبود، شاید بدنه‌ی پاسخ مستقیماً همون توکنه
                return string.IsNullOrWhiteSpace(rawResponse) ? null : rawResponse;
            }

            return null;
        }

        /// <summary>
        /// تبدیل تاریخ شمسی به شکل "1401/01/07" به DateTime میلادی. اگه فرمت نامعتبر بود null برمی‌گردونه
        /// (throw نمی‌کنه چون این فقط یه فیلد کمکیه، نباید کل ثبت قبض انبار رو خراب کنه).
        /// </summary>
        private DateTime? ParsePersianDateSafe(string? persianDate)
        {
            if (string.IsNullOrWhiteSpace(persianDate))
                return null;

            var parts = persianDate.Split('/');
            if (parts.Length != 3)
                return null;

            try
            {
                var pc = new System.Globalization.PersianCalendar();
                int year = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);
                int day = int.Parse(parts[2]);
                return pc.ToDateTime(year, month, day, 0, 0, 0, 0);
            }
            catch
            {
                return null;
            }
        }

        private string GetDocumentText(string responseString)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("responseText", out var responseTextElement) &&
                    responseTextElement.ValueKind == JsonValueKind.String)
                {
                    using var nestedDoc = JsonDocument.Parse(responseTextElement.GetString() ?? "{}");
                    if (nestedDoc.RootElement.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("data", out var dataElement))
                    {
                        // سعی می‌کنیم توضیحات سند (Desc) یا متن اصلی را پیدا کنیم
                        if (dataElement.TryGetProperty("Desc", out var descElement))
                            return descElement.GetString() ?? "توضیحی در سند وجود ندارد.";

                        if (dataElement.TryGetProperty("ImpotrtantAnnexText", out var annexElement))
                            return annexElement.GetString() ?? "ضمیمه مهمی یافت نشد.";

                        if (dataElement.TryGetProperty("DocImage_Base64", out var imgElement))
                            return "[سند دارای تصویر است - داده Base64]";
                    }
                }

                return "متن یا توضیحات سند در پاسخ یافت نشد.";
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "خطا در استخراج متن سند از پاسخ VerifyDoc");
                return $"خطا در پردازش سند: {ex.Message}";
            }
        }

        [HttpGet("GetTextByRequestId/{requestId}")]
        public async Task<DocTextResponse> GetTextByRequestId(long requestId)
        {
            try
            {
                // جستجو در جدول VerifyDocLog برای یافتن رکورد مرتبط با RequestId
                var verifyDocLog = await _context.VerifyDocLog
                    .Where(x => x.RequestId == requestId)
                    .Select(x => new
                    {
                        ResponseText = x.ResponseText,
                        IsRead = x.IsRead ?? false,
                        ExistDoc = x.IsExist ?? false
                    })
                    .FirstOrDefaultAsync();

                // اگر هیچ رکوردی یافت نشد
                if (verifyDocLog == null)
                {
                    return new DocTextResponse
                    {
                        Success = false,
                        DocumentText = null,
                        IsRead = false,
                        ExistDoc = false,
                        Message = "سندی برای این درخواست یافت نشد."
                    };
                }

                // اگر ResponseText خالی باشد
                if (string.IsNullOrEmpty(verifyDocLog.ResponseText))
                {
                    return new DocTextResponse
                    {
                        Success = false,
                        DocumentText = null,
                        IsRead = verifyDocLog.IsRead,
                        ExistDoc = verifyDocLog.ExistDoc,
                        Message = "محتوای پاسخ سند خالی است."
                    };
                }

                try
                {
                    // پارست کردن ResponseText
                    using var outerDoc = JsonDocument.Parse(verifyDocLog.ResponseText);
                    var outerRoot = outerDoc.RootElement;

                    string? innerResponseText = null;
                    if (outerRoot.TryGetProperty("responseText", out var responseTextElement))
                    {
                        innerResponseText = responseTextElement.GetString();
                    }

                    if (string.IsNullOrEmpty(innerResponseText))
                    {
                        return new DocTextResponse
                        {
                            Success = false,
                            DocumentText = null,
                            IsRead = verifyDocLog.IsRead,
                            ExistDoc = verifyDocLog.ExistDoc,
                            Message = "داده‌ای در پاسخ یافت نشد."
                        };
                    }

                    // پارست کردن innerResponseText
                    using var jsonDoc = JsonDocument.Parse(innerResponseText);
                    var root = jsonDoc.RootElement;

                    if (!root.TryGetProperty("result", out var resultElement) ||
                        !resultElement.TryGetProperty("data", out var dataElement))
                    {
                        return new DocTextResponse
                        {
                            Success = false,
                            DocumentText = null,
                            IsRead = verifyDocLog.IsRead,
                            ExistDoc = verifyDocLog.ExistDoc,
                            Message = "داده‌ای در پاسخ یافت نشد."
                        };
                    }

                    string? importantText = dataElement.TryGetProperty("ImpotrtantAnnexText", out var annexText)
                        ? annexText.GetString()
                        : null;

                    if (string.IsNullOrEmpty(importantText))
                    {
                        return new DocTextResponse
                        {
                            Success = false,
                            DocumentText = null,
                            IsRead = verifyDocLog.IsRead,
                            ExistDoc = verifyDocLog.ExistDoc,
                            Message = "متنی برای این سند وجود ندارد."
                        };
                    }

                    return new DocTextResponse
                    {
                        Success = true,
                        DocumentText = importantText,
                        IsRead = verifyDocLog.IsRead,
                        ExistDoc = verifyDocLog.ExistDoc,
                        Message = null
                    };
                }
                catch (JsonException ex)
                {
                    return new DocTextResponse
                    {
                        Success = false,
                        DocumentText = null,
                        IsRead = verifyDocLog.IsRead,
                        ExistDoc = verifyDocLog.ExistDoc,
                        Message = $"خطا در پردازش JSON: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new DocTextResponse
                {
                    Success = false,
                    DocumentText = null,
                    IsRead = false,
                    ExistDoc = false,
                    Message = $"خطا در دریافت اطلاعات سند: {ex.Message}"
                };
            }
        }

        [HttpGet("GetWarehouseReceiptByRequestId/{requestId}")]
        [Authorize(Policy = "CanAccessServices")]
        public async Task<IActionResult> GetWarehouseReceiptByRequestId(long requestId)
        {
            try
            {
                var receipts = await _context.WarehouseReceipts
                    .Where(w => w.RequestId == requestId)
                    .OrderBy(w => w.WarehouseReceiptId)
                    .ToListAsync();

                if (receipts.Count == 0)
                {
                    return new JsonResult(new { success = false, message = "اطلاعاتی برای قبض انبار این درخواست یافت نشد." });
                }

                var receiptNumber = receipts.First().ReceiptNumber;
                var isVerified = receipts.Any(r => r.IsVerified == true);

                var lines = new List<string>();
                int i = 1;
                foreach (var r in receipts)
                {
                    lines.Add($"— قلم {i++} —");
                    lines.Add($"شماره قبض انبار: {r.ReceiptNumber}");
                    if (!string.IsNullOrWhiteSpace(r.SerialNumber)) lines.Add($"شماره فاکتور: {r.SerialNumber}");
                    if (!string.IsNullOrWhiteSpace(r.OwnerNationalId)) lines.Add($"کد ملی/اقتصادی صاحب کالا: {r.OwnerNationalId}");
                    if (r.Quantity.HasValue) lines.Add($"وزن: {r.Quantity} {r.Unit}");
                    if (r.IssueDate.HasValue) lines.Add($"تاریخ تخلیه: {r.IssueDate:yyyy/MM/dd}");
                    lines.Add($"وضعیت: {(r.IsVerified == true ? "تایید شده" : "تایید نشده")}");
                    lines.Add("");
                }

                return new JsonResult(new
                {
                    success = true,
                    receiptNumber,
                    isVerified,
                    text = string.Join("\n", lines)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت قبض انبار برای درخواست {RequestId}", requestId);
                return new JsonResult(new { success = false, message = $"خطا در دریافت اطلاعات قبض انبار: {ex.Message}" });
            }
        }

        [HttpPost("MarkDocumentAsRead/{requestId}")]
        [Authorize(Policy = "CanAccessServices")]
        public async Task<IActionResult> MarkDocumentAsRead(long requestId)
        {
            try
            {
                _logger.LogInformation("Processing MarkDocumentAsRead for RequestId: {RequestId}", requestId);

                var verifyDocLog = await _context.VerifyDocLog
                    .Where(v => v.RequestId == requestId)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (verifyDocLog == null)
                {
                    _logger.LogWarning("No VerifyDocLog found for RequestId: {RequestId}", requestId);
                    return new JsonResult(new { success = false, message = "سندی با این شناسه یافت نشد." }) { StatusCode = 404 };
                }

                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    _logger.LogWarning("Could not extract UserId from JWT token.");
                    return new JsonResult(new { success = false, message = "کاربر شناسایی نشد." }) { StatusCode = 401 };
                }

                verifyDocLog.IsRead = true;
                verifyDocLog.ReadBy = User.Identity?.Name ?? userId.ToString();
                verifyDocLog.ReadDate = DateTime.UtcNow;

                _context.VerifyDocLog.Update(verifyDocLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document marked as read for RequestId: {RequestId} by User: {User}", requestId, verifyDocLog.ReadBy);
                return new JsonResult(new { success = true, message = "سند با موفقیت به عنوان خوانده شده علامت‌گذاری شد." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking document as read for RequestId: {RequestId}. InnerException: {InnerException}", requestId, ex.InnerException?.Message);
                return new JsonResult(new { success = false, message = $"خطا در علامت‌گذاری سند: {ex.Message}{(ex.InnerException != null ? " - " + ex.InnerException.Message : "")}" }) { StatusCode = 500 };
            }
        }

        private string GenerateRequestId()
        {
            var now = DateTime.Now;
            string dateTimePart = now.ToString("yyyyMMddHHmmss");
            string microseconds = (now.Ticks % 1000000).ToString("D6");
            return $"{_providerCode}{dateTimePart}{microseconds}";
        }

        private async Task<bool> CanMakeShahkarRequest(long expertId)
        {
            var lastHour = DateTime.UtcNow.AddHours(-1);
            var requestCount = await _context.ShahkarLog
                .CountAsync(r => r.ExpertId == expertId && r.CreatedAt > lastHour);
            _logger.LogInformation("Request count for expert {ExpertId} in last hour: {RequestCount}", expertId, requestCount);
            return requestCount < 10;
        }

        private bool EqualsNormalized(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            return a.Trim().Replace("ي", "ی").Replace("ك", "ک") ==
                   b.Trim().Replace("ي", "ی").Replace("ك", "ک");
        }

        private bool IsValidIranianNationalId(string nationalId)
        {
            if (nationalId.Length != 10 || !nationalId.All(char.IsDigit)) return false;
            int[] digits = nationalId.Select(c => int.Parse(c.ToString())).ToArray();
            int checkDigit = digits[9];
            int sum = Enumerable.Range(0, 9).Sum(i => digits[i] * (10 - i));
            int remainder = sum % 11;
            return remainder < 2 ? checkDigit == remainder : checkDigit == 11 - remainder;
        }

        private string GetJwtToken() => "your-valid-token";
    }

    #region ViewModels & DTOs

    public class DocTextResponse
    {
        public bool Success { get; set; }
        public string? DocumentText { get; set; }
        public bool IsRead { get; set; }
        public bool ExistDoc { get; set; }
        public string? Message { get; set; }
    }

    public class CombinedRequestViewModel
    {
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public string? WarehouseReceiptNumber { get; set; }
    }

    public class BsrServiceOptions
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>نتیجه‌ی یک فراخوانی سرویس bsr-* — هیچوقت throw نمی‌شه، خطا این‌جا منعکس می‌شه.</summary>
    public class BsrServiceCallResult
    {
        public bool IsSuccessful { get; set; }
        public string RawResponseText { get; set; } = string.Empty;
        public string? InnerResponseText { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// یک قلم از آرایه‌ی پاسخ bsr-GetPortIncomeInvoice (responseText). فقط فیلدهایی که
    /// در پروژه استفاده می‌شن تعریف شدن؛ بقیه‌ی فیلدهای واقعی پاسخ نادیده گرفته می‌شن.
    /// </summary>
    public class PortIncomeInvoiceItem
    {
        public string? TrafficType { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceDate { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Vessel { get; set; }
        public string? BLNo { get; set; }
        public decimal? Weight { get; set; }
        public string? DischargeDate { get; set; }
        public string? CustomsDecNumber { get; set; }
        public string? GoodsOwnerName { get; set; }
        public string? GoodsOwnerNationalID { get; set; }
        public decimal? Total { get; set; }
    }

    public class InternalShahkarResponse
    {
        public ResultWrapper? Result { get; set; }
    }

    public class ResultWrapper
    {
        public DataWrapper? Data { get; set; }
        public StatusWrapper? Status { get; set; }
    }

    public class VerifyDocResponse
    {
        public bool IsSuccessful { get; set; }
        public string? ResponseText { get; set; }
        public List<PersonInQuery>? PersonsInQuery { get; set; }
        public bool ExistDoc { get; set; }
        public bool IsNationalIdInLawyers { get; set; }
        public bool IsNationalIdInResponse { get; set; }
    }

    public class DataWrapper
    {
        public string? Result { get; set; }
        public string? RequestId { get; set; }
        public int Response { get; set; }
        public string? Comment { get; set; }
        public string? Id { get; set; }
    }

    public class StatusWrapper
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
    }

    public class ShahkarServiceOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public Credential Credential { get; set; } = new Credential();
    }

    public class Credential
    {
        public string Code { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ShahkarResponse
    {
        [JsonPropertyName("isSuccessful")]
        public bool IsSuccessful { get; set; }

        [JsonPropertyName("error")]
        public int Error { get; set; }

        [JsonPropertyName("errorDescription")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("responseText")]
        public string? ResponseText { get; set; }

        [JsonPropertyName("responseStatusCode")]
        public int ResponseStatusCode { get; set; }

        [JsonPropertyName("usedQuotaStats")]
        public UsedQuotaStats? UsedQuotaStats { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }

    public class UsedQuotaStats
    {
        [JsonPropertyName("hourlyUsed")]
        public int HourlyUsed { get; set; }

        [JsonPropertyName("dailyUsed")]
        public int DailyUsed { get; set; }

        [JsonPropertyName("monthlyUsed")]
        public int MonthlyUsed { get; set; }
    }

    public class VerifyDocInternalResponse
    {
        public VerifyDocResultWrapper? Result { get; set; }
        public StatusWrapper? Status { get; set; }
    }

    public class VerifyDocResultWrapper
    {
        public JsonElement Data { get; set; }
        public StatusWrapper? Status { get; set; }
    }

    public class VerifyDocDataWrapper
    {
        public List<object>? RegCases { get; set; }
        public List<object>? BaseDocuments { get; set; }
        public List<object>? FollowerDocuments { get; set; }
        public bool Succseed { get; set; }
        public string? NationalRegisterNo { get; set; }
        public string? DocType { get; set; }
        public string? DocType_code { get; set; }
        public bool HasPermission { get; set; }
        public bool ExistDoc { get; set; }
        public string? Desc { get; set; }
        public string? ScriptoriumName { get; set; }
        public string? SignGetterTitle { get; set; }
        public string? SignSubject { get; set; }
        public string? DocDate { get; set; }
        public string? CaseClasifyNo { get; set; }
        public string? ImpotrtantAnnexText { get; set; }
        public string? DocImage { get; set; }
        public string? DocImage_Base64 { get; set; }
        public string? ADVOCACYENDDATE { get; set; }
        public List<PersonInQuery>? LstFindPersonInQuery { get; set; }
    }

    public class PersonInQuery
    {
        public string? NationalNo { get; set; }
        public string? Birthdate { get; set; }
        public string? Name { get; set; }
        public string? Family { get; set; }
        public string? AgentType { get; set; }
        public string? PersonType { get; set; }
        public string? PersonType_code { get; set; }
        public string? NationalNoMovakel { get; set; }
        public string? NameMovakel { get; set; }
        public string? FamilyMovakel { get; set; }
        public string? TxtRelation { get; set; }
        public string? RoleType { get; set; }
        public string? Person_RoleType_code { get; set; }
    }
    #endregion
}