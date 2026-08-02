using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityManagementSystem.API.Services.Sms
{
    /// <summary>
    /// کلاینت سرویس داخلی پیامک سازمانی (همون پنلی که در FavaReminderService استفاده می‌شه).
    /// درخواست به شکل GET با appName/to/msg در query string ارسال می‌شه؛
    /// موفقیت با وجود واژه "success" در پاسخ متنی تشخیص داده می‌شه.
    /// این کلاس فقط مسئول تماس با پنله؛ ثبت لاگ در جدول Log.Sms بر عهده‌ی caller (کنترلره).
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SmsService> _logger;
        private readonly string? _baseUrl;
        private readonly string? _appName;

        public SmsService(HttpClient httpClient, IConfiguration configuration, ILogger<SmsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["Sms:BaseUrl"];
            _appName = configuration["Sms:AppName"];
        }

        public async Task<SmsResult> SendAsync(string to, string message)
        {
            if (string.IsNullOrWhiteSpace(to))
                return new SmsResult { IsSuccess = false, ErrorMessage = "شماره مقصد خالی است." };

            if (string.IsNullOrWhiteSpace(message))
                return new SmsResult { IsSuccess = false, ErrorMessage = "متن پیام خالی است." };

            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                _logger.LogWarning("Sms:BaseUrl تنظیم نشده است؛ پیامک ارسال نشد.");
                return new SmsResult { IsSuccess = false, ErrorMessage = "Sms:BaseUrl تنظیم نشده است." };
            }

            var encodedAppName = WebUtility.UrlEncode(_appName ?? string.Empty);
            var encodedTo = WebUtility.UrlEncode(to);
            var encodedMsg = WebUtility.UrlEncode(message);
            var fullUrl = $"{_baseUrl.TrimEnd('/')}?appName={encodedAppName}&to={encodedTo}&msg={encodedMsg}";
            // مطابق پیاده‌سازی مرجع (SendSmsService) کل URL یه‌بار decode می‌شه چون پنل متن خام (نه percent-encoded) انتظار داره
            var decodedUrl = WebUtility.UrlDecode(fullUrl);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                var response = await _httpClient.GetAsync(decodedUrl, cts.Token);
                var result = await response.Content.ReadAsStringAsync(cts.Token);
                result ??= string.Empty;

                var isSuccess = response.IsSuccessStatusCode &&
                                 result.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isSuccess)
                {
                    _logger.LogWarning("ارسال پیامک به {To} ناموفق بود. StatusCode={StatusCode}, Response={Response}",
                        to, response.StatusCode, result);
                }

                return new SmsResult { IsSuccess = isSuccess, RawResponse = result };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("ارسال پیامک به {To} به دلیل timeout ناموفق بود.", to);
                return new SmsResult { IsSuccess = false, ErrorMessage = "Timeout در ارتباط با سرویس پیامک." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در ارسال پیامک به {To}", to);
                return new SmsResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
    }
}
