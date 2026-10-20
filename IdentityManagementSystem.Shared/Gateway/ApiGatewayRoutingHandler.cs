using Microsoft.Extensions.Configuration;

namespace IdentityManagementSystem.Shared.Gateway
{
    /// <summary>
    /// هر درخواست خروجی UI/PublicPortal به API (مثلاً api/Auth/login) رو بر اساس ApiSettings:Mode مسیردهی می‌کنه،
    /// بدون اینکه کد کنترلرها عوض بشه:
    ///   Pomix         → POST {PomixUrls:auth-handler}?action=Auth/login
    ///   DirectGateway → POST {URL}/api/auth-handler?action=Auth/login
    ///   DirectApi     → همون درخواست اصلی به {URL}/api/Auth/login
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null)
            {
                var settings = _configuration.GetSection("ApiSettings");
                var mode = ApiGatewayModes.Parse(settings["Mode"], ApiGatewayMode.DirectApi);
                Route(request, settings, mode);
            }

            return base.SendAsync(request, cancellationToken);
        }

        private static void Route(HttpRequestMessage request, IConfigurationSection settings, ApiGatewayMode mode)
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

            // action اول، بعد query string خودِ آدرس هندلر (اگه داشت) و query اصلی درخواست
            var queryParts = new List<string> { $"{ApiGatewayRoutes.ActionParameter}={Uri.EscapeDataString(action)}" };
            if (target.Query.Length > 1) queryParts.Add(target.Query.TrimStart('?'));
            if (uri.Query.Length > 1) queryParts.Add(uri.Query.TrimStart('?'));
            target.Query = string.Join("&", queryParts);

            request.RequestUri = target.Uri;

            // هندلرها فقط POST قبول می‌کنن؛ API متد واقعی رو خودش از route پیدا می‌کنه. هدر override فقط برای وقتیه
            // که یک مسیر چند متد داشته باشه.
            if (request.Method != HttpMethod.Post)
            {
                request.Headers.Remove(ApiGatewayRoutes.MethodOverrideHeader);
                request.Headers.TryAddWithoutValidation(ApiGatewayRoutes.MethodOverrideHeader, request.Method.Method);
                request.Method = HttpMethod.Post;
            }
        }
    }
}
