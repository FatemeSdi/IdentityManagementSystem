using System.Security.Cryptography;
using System.Text;

namespace IdentityManagementSystem.API.Gateway
{
    /// <summary>
    /// فقط UI و PublicPortal خودمون (که هدر X-API-Key رو با کلید appsettings.Secrets.json می‌فرستن) اجازه‌ی صدا زدن /api/* رو دارن.
    /// حالا که API از طریق Gateway سازمان قابل دسترسه، این جلوی فراخوانی مستقیم هندلرها توسط بقیه رو می‌گیره.
    /// اگه ApiKey تنظیم نشده باشه چک انجام نمی‌شه (فقط هشدار تو لاگ) تا محیط توسعه بدون فایل secrets هم بالا بیاد.
    /// </summary>
    public class ApiKeyMiddleware
    {
        public const string HeaderName = "X-API-Key";

        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private bool _warnedMissingKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var expectedKey = _configuration["ApiKey"];
            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                if (!_warnedMissingKey)
                {
                    _logger.LogWarning("ApiKey تنظیم نشده — چک X-API-Key غیرفعاله. روی سرور حتماً در appsettings.Secrets.json مقدار بدید.");
                    _warnedMissingKey = true;
                }
                await _next(context);
                return;
            }

            var providedKey = context.Request.Headers[HeaderName].ToString();
            if (!FixedTimeEquals(providedKey, expectedKey))
            {
                _logger.LogWarning("درخواست بدون X-API-Key معتبر رد شد: {Method} {Path} از {Ip}",
                    context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "کلید دسترسی (X-API-Key) نامعتبر است." });
                return;
            }

            await _next(context);
        }

        private static bool FixedTimeEquals(string provided, string expected) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
    }
}
