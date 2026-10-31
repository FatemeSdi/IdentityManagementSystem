using Microsoft.Extensions.Configuration;

namespace IdentityManagementSystem.Shared.Gateway
{
    /// <summary>
    /// هر درخواست خروجی UI/PublicPortal به API (مثلاً api/Auth/login) رو بر اساس ApiSettings:Mode مسیردهی می‌کنه،
    /// بدون اینکه کد کنترلرها عوض بشه:
    ///   Pomix         → POST {PomixUrls:auth-handler}     با body: {"action":"Auth/login","method":"POST","query":{},"body":{...}}
    ///   DirectGateway → POST {URL}/api/auth-handler       با همون body
    ///   DirectApi     → همون درخواست اصلی به {URL}/api/Auth/login
    /// (ساختار body در ApiGatewayEnvelope)
    ///
    /// تنظیمات (بخش ApiSettings):
    ///   Mode       — Pomix | DirectGateway | DirectApi (نبودنش = DirectApi، رفتار قبلی)
    ///   URL        — آدرس مستقیم سرور API (برای DirectGateway و DirectApi)
    ///   PomixUrls  — آدرس هر سه هندلر روی Gateway سازمان (برای Pomix)
    /// همه‌ی این‌ها تو هر درخواست از کانفیگ خونده می‌شن، پس عوض کردنشون تو appsettings.json بدون restart اعمال می‌شه.
    /// </summary>
    public class ApiGatewayRoutingHandler : DelegatingHandler
    {
        private const string ApiSegment = "/api/";
        private readonly IConfiguration _configuration;

        public ApiGatewayRoutingHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null)
            {
                var settings = _configuration.GetSection("ApiSettings");
                var mode = ApiGatewayModes.Parse(settings["Mode"], ApiGatewayMode.DirectApi);
                await Route(request, settings, mode, cancellationToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static async Task Route(HttpRequestMessage request, IConfigurationSection settings, ApiGatewayMode mode, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var path = uri.AbsolutePath;
            var apiIndex = path.IndexOf(ApiSegment, StringComparison.OrdinalIgnoreCase);
            if (apiIndex < 0)
                return;

            var relativePath = path[(apiIndex + ApiSegment.Length)..];
            var directUrl = settings["URL"]?.Trim().TrimEnd('/');

            if (mode == ApiGatewayMode.DirectApi)
            {
                // آدرس از کانفیگِ فعلی خونده می‌شه تا تغییر URL هم بدون restart اعمال بشه
                if (!string.IsNullOrEmpty(directUrl))
                    request.RequestUri = new Uri($"{directUrl}{ApiSegment}{relativePath}{uri.Query}");
                return;
            }

            var action = Uri.UnescapeDataString(relativePath).TrimEnd('/');
            var handler = ApiGatewayRoutes.FindHandlerForController(action.Split('/')[0]);
            if (handler == null)
                return; // مسیری که پشت هیچ هندلری نیست دست‌نخورده می‌ره

            UriBuilder target;
            if (mode == ApiGatewayMode.Pomix)
            {
                var pomixUrl = settings[$"PomixUrls:{handler}"];
                if (string.IsNullOrWhiteSpace(pomixUrl))
                    throw new InvalidOperationException($"ApiSettings:Mode = Pomix است ولی آدرس ApiSettings:PomixUrls:{handler} تنظیم نشده.");
                target = new UriBuilder(pomixUrl.Trim());
            }
            else
            {
                target = string.IsNullOrEmpty(directUrl)
                    ? new UriBuilder(uri) { Path = path[..apiIndex] + ApiSegment + handler, Query = string.Empty }
                    : new UriBuilder($"{directUrl}{ApiSegment}{handler}");
            }

            // action، متد و query اصلی همه توی body (envelope) می‌رن، نه query string یا هدر — Pomix فقط body رو
            // دست‌نخورده عبور می‌ده. query string خودِ آدرس هندلر (اگه تو PomixUrls داشت) دست‌نخورده می‌مونه.
            string? bodyJson = null;
            if (request.Content != null)
            {
                var mediaType = request.Content.Headers.ContentType?.MediaType;
                bodyJson = await request.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(bodyJson) && mediaType != null && !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"هندلرهای Gateway فقط body از نوع JSON را پشتیبانی می‌کنند (درخواست {action} با {mediaType}).");
                request.Content.Dispose();
            }

            var envelope = ApiGatewayEnvelope.Build(action, request.Method.Method, uri.Query, bodyJson);
            request.Content = new ByteArrayContent(envelope);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ApiGatewayEnvelope.ContentType) { CharSet = "utf-8" };

            request.RequestUri = target.Uri;
            request.Method = HttpMethod.Post;
        }
    }
}
