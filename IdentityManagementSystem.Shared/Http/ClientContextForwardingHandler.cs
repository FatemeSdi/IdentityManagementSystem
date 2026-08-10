using Microsoft.AspNetCore.Http;

namespace IdentityManagementSystem.Shared.Http
{
    /// <summary>
    /// وقتی UI (کارشناس یا متقاضی) سرور-به-سرور با API حرف می‌زنه، به‌طور پیش‌فرض API فقط IP/UserAgent
    /// خودِ سرور UI رو می‌بینه، نه کاربر واقعی مرورگر رو. این هندلر IP و User-Agent واقعیِ کاربر رو
    /// (از HttpContext همون درخواستی که UI داره پردازش می‌کنه) روی هر درخواست خروجی به API ست می‌کنه،
    /// تا لاگ‌های UserLog تو سمت API اطلاعات واقعی کاربر رو نشون بدن.
    /// </summary>
    public class ClientContextForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientContextForwardingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(ip))
                {
                    request.Headers.Remove("X-Forwarded-For");
                    request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
                }

                var userAgent = ctx.Request.Headers["User-Agent"].ToString();
                if (!string.IsNullOrEmpty(userAgent))
                {
                    request.Headers.Remove("User-Agent");
                    request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
