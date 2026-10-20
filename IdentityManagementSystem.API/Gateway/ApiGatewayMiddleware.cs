using IdentityManagementSystem.Shared.Gateway;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace IdentityManagementSystem.API.Gateway
{
    /// <summary>
    /// نقطه‌ی ورود هندلرها (auth-handler / ehraz-handler / service-handler). هر هندلر فقط با POST صدا زده می‌شه:
    ///   POST /api/{handler}?action=X/Y
    /// متد واقعی سرویس (GET/PUT/DELETE/POST) از روی تعریف route همون سرویس پیدا می‌شه و درخواست قبل از Routing
    /// به "{متد واقعی} /api/X/Y" بازنویسی می‌شه، پس همون کنترلر، [Authorize] و Policy قبلی اجرا می‌شه و
    /// body / هدرها / بقیه‌ی query string دست‌نخورده می‌مونن. (اگه یک مسیر چند متد داشته باشه، با هدر X-HTTP-Method-Override انتخاب می‌شه.)
    /// درخواست به هندلر بدون action، فهرست سرویس‌های پشت اون هندلر رو برمی‌گردونه.
    /// Gateway:AllowedModes (Pomix / DirectGateway / DirectApi) مشخص می‌کنه کدوم مسیرها باز باشن: بدون DirectApi مسیرهای مستقیم
    /// (/api/Auth/login و ...) 404 می‌دن؛ بدون DirectGateway هندلرها فقط از IP های Gateway سازمان (Gateway:PomixSourceIps) قبول می‌شن.
    /// </summary>
    public class ApiGatewayMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiGatewayMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private bool _warnedNoPomixIps;

        public ApiGatewayMiddleware(RequestDelegate next, ILogger<ApiGatewayMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>کلید HttpContext.Items برای IP واقعی اتصال (قبل از اعمال X-Forwarded-For که قابل جعله).</summary>
        public const string ConnectionIpItemKey = "Gateway.ConnectionIp";

        public async Task InvokeAsync(HttpContext context, EndpointDataSource endpointDataSource)
        {
            var allowedModes = GetAllowedModes();
            var handler = GetHandlerName(context.Request.Path);
            if (handler == null)
            {
                if (!allowedModes.Contains(ApiGatewayMode.DirectApi) &&
                    context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "دسترسی مستقیم بسته است؛ از هندلرها استفاده کنید.",
                        handlers = ApiGatewayRoutes.Handlers.Keys.Select(h => $"POST /api/{h}")
                    });
                    return;
                }

                await _next(context);
                return;
            }

            if (!IsHandlerAccessAllowed(context, allowedModes))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "دسترسی به هندلرها از این مسیر مجاز نیست." });
                return;
            }

            var request = context.Request;
            string? action = request.Query[ApiGatewayRoutes.ActionParameter];
            if (string.IsNullOrWhiteSpace(action))
                action = request.Headers[ApiGatewayRoutes.ActionHeader];
            action = action?.Trim().Trim('/');

            if (string.IsNullOrEmpty(action))
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    handler,
                    usage = $"POST /api/{handler}?{ApiGatewayRoutes.ActionParameter}=<Controller>/<route>",
                    services = ListServices(handler, endpointDataSource)
                });
                return;
            }

            if (!HttpMethods.IsPost(request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                context.Response.Headers.Allow = HttpMethods.Post;
                await context.Response.WriteAsJsonAsync(new { message = $"هندلر '{handler}' فقط با POST صدا زده می‌شود." });
                return;
            }

            if (!ApiGatewayRoutes.IsActionAllowed(handler, action))
            {
                await WriteNotFound(context, handler, action);
                return;
            }

            var methods = FindMethods(action, endpointDataSource);
            if (methods.Count == 0)
            {
                await WriteNotFound(context, handler, action);
                return;
            }

            var methodOverride = request.Headers[ApiGatewayRoutes.MethodOverrideHeader].ToString().Trim();
            string targetMethod;
            if (methods.Contains(methodOverride))
                targetMethod = methodOverride.ToUpperInvariant();
            else if (methods.Count == 1)
                targetMethod = methods.First();
            else if (methods.Contains(HttpMethods.Post))
                targetMethod = HttpMethods.Post;
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = $"سرویس '{action}' چند متد دارد؛ متد مورد نظر را در هدر {ApiGatewayRoutes.MethodOverrideHeader} بفرستید.",
                    methods
                });
                return;
            }

            var remainingQuery = QueryString.Create(request.Query
                .Where(q => !string.Equals(q.Key, ApiGatewayRoutes.ActionParameter, StringComparison.OrdinalIgnoreCase))
                .SelectMany(q => q.Value.Select(v => new KeyValuePair<string, string?>(q.Key, v))));

            _logger.LogDebug("Gateway {Handler}: {Method} {Action}{Query}", handler, targetMethod, action, remainingQuery);

            request.Method = targetMethod;
            request.Path = "/api/" + action;
            request.QueryString = remainingQuery;

            await _next(context);
        }

        /// <summary>
        /// Gateway:AllowedModes (هر درخواست از کانفیگ خونده می‌شه، پس بدون restart قابل تغییره). نبودنش = هر سه حالت مجاز.
        /// </summary>
        private HashSet<ApiGatewayMode> GetAllowedModes()
        {
            // رشته‌ی جدا با کاما (نه آرایه‌ی JSON) چون آرایه‌ها بین appsettings.json و appsettings.Production.json اندیس‌به‌اندیس ادغام می‌شن
            var values = SplitList(_configuration["Gateway:AllowedModes"]);
            if (values.Length == 0)
                return new HashSet<ApiGatewayMode>(Enum.GetValues<ApiGatewayMode>());

            return values.Select(v => ApiGatewayModes.Parse(v, ApiGatewayMode.DirectApi)).ToHashSet();
        }

        /// <summary>
        /// DirectGateway مجاز باشه → هر کسی (با X-API-Key معتبر) می‌تونه هندلرها رو صدا بزنه.
        /// فقط Pomix مجاز باشه → درخواست باید از یکی از IP های Gateway سازمان (Gateway:PomixSourceIps) اومده باشه.
        /// </summary>
        private bool IsHandlerAccessAllowed(HttpContext context, HashSet<ApiGatewayMode> allowedModes)
        {
            if (allowedModes.Contains(ApiGatewayMode.DirectGateway))
                return true;
            if (!allowedModes.Contains(ApiGatewayMode.Pomix))
                return false;

            var pomixIps = SplitList(_configuration["Gateway:PomixSourceIps"]);
            if (pomixIps.Length == 0)
            {
                // بدون فهرست IP نمی‌شه Pomix رو از اتصال مستقیم تشخیص داد
                if (!_warnedNoPomixIps)
                {
                    _logger.LogWarning("Gateway:AllowedModes فقط Pomix است ولی Gateway:PomixSourceIps خالیه — همه‌ی درخواست‌های هندلر قبول می‌شن.");
                    _warnedNoPomixIps = true;
                }
                return true;
            }

            var connectionIp = Normalize(context.Items[ConnectionIpItemKey] as System.Net.IPAddress ?? context.Connection.RemoteIpAddress);
            var allowed = pomixIps.Any(ip => System.Net.IPAddress.TryParse(ip.Trim(), out var parsed) && Normalize(parsed)!.Equals(connectionIp));
            if (!allowed)
                _logger.LogWarning("درخواست هندلر از IP غیر Pomix رد شد: {Ip} {Path}", connectionIp, context.Request.Path);
            return allowed;
        }

        private static string[] SplitList(string? value) =>
            (value ?? string.Empty).Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static System.Net.IPAddress? Normalize(System.Net.IPAddress? ip) =>
            ip != null && ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;

        private static Task WriteNotFound(HttpContext context, string handler, string action)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return context.Response.WriteAsJsonAsync(new
            {
                message = $"سرویس '{action}' پشت هندلر '{handler}' تعریف نشده است.",
                controllers = ApiGatewayRoutes.Handlers[handler]
            });
        }

        private static string? GetHandlerName(PathString path)
        {
            if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase, out var rest))
                return null;

            var name = rest.Value?.Trim('/');
            return !string.IsNullOrEmpty(name) && ApiGatewayRoutes.Handlers.ContainsKey(name) ? name.ToLowerInvariant() : null;
        }

        /// <summary>متدهای HTTP همه‌ی endpoint هایی که route شون با /api/{action} جور درمیاد.</summary>
        private static HashSet<string> FindMethods(string action, EndpointDataSource endpointDataSource)
        {
            var segments = ("api/" + action).Split('/', StringSplitOptions.RemoveEmptyEntries);
            var methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var endpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
            {
                if (!Matches(endpoint.RoutePattern, segments))
                    continue;

                var endpointMethods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
                if (endpointMethods != null)
                    methods.UnionWith(endpointMethods);
            }

            return methods;
        }

        /// <summary>
        /// تطبیق ساده‌ی مسیر با الگوی route: بخش‌های ثابت باید یکی باشن (بدون حساسیت به حروف) و هر پارامتر ({id}) با یک بخش غیرخالی جور میاد.
        /// </summary>
        private static bool Matches(RoutePattern pattern, string[] segments)
        {
            var patternSegments = pattern.PathSegments;
            if (segments.Length > patternSegments.Count)
                return false;

            for (var i = 0; i < patternSegments.Count; i++)
            {
                var parts = patternSegments[i].Parts;
                var isLiteral = parts.Count == 1 && parts[0] is RoutePatternLiteralPart;

                if (i >= segments.Length)
                {
                    // بخش‌های باقی‌مونده فقط وقتی مشکلی ندارن که پارامتر اختیاری باشن
                    if (isLiteral || parts.Any(p => p is RoutePatternParameterPart { IsOptional: false, Default: null }))
                        return false;
                    continue;
                }

                if (isLiteral && !string.Equals(((RoutePatternLiteralPart)parts[0]).Content, segments[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static IEnumerable<object> ListServices(string handler, EndpointDataSource endpointDataSource)
        {
            var controllers = ApiGatewayRoutes.Handlers[handler];
            return endpointDataSource.Endpoints
                .OfType<RouteEndpoint>()
                .Select(e => new
                {
                    Route = e.RoutePattern.RawText?.TrimStart('/') ?? string.Empty,
                    Methods = e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? Array.Empty<string>()
                })
                .Where(e => e.Route.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                .Select(e => new { action = e.Route[4..], e.Methods })
                .Where(e => controllers.Contains(e.action.Split('/')[0], StringComparer.OrdinalIgnoreCase))
                .OrderBy(e => e.action)
                .Select(e => (object)new { e.action, originalMethod = string.Join(",", e.Methods) });
        }
    }
}
