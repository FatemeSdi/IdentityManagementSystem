namespace IdentityManagementSystem.Shared.Gateway
{
    /// <summary>
    /// زیرساخت سازمان هر URL رو جدا باید هاست کنه؛ برای اینکه به‌جای +۴۰ سرویس فقط چند آدرس ثابت منتشر بشه،
    /// همه‌ی کنترلرهای API پشت این چند هندلر (gateway) قرار می‌گیرن. سرویس مقصد با پارامتر action مشخص می‌شه:
    /// همه با POST (متد واقعی سرویس رو API از route پیدا می‌کنه):
    ///   POST /api/auth-handler?action=Auth/login
    ///   POST /api/ehraz-handler?action=Request/Track&amp;trackingCode=...&amp;mobileNumber=...
    ///   POST /api/service-handler?action=Service/GetTextByRequestId/101
    /// هم سمت API (ApiGatewayMiddleware) و هم سمت UI/PublicPortal (ApiGatewayRoutingHandler) از همین جدول استفاده می‌کنن.
    /// </summary>
    public static class ApiGatewayRoutes
    {
        /// <summary>نام پارامتر query (یا هدر X-Gateway-Action) که مسیر سرویس مقصد رو نگه می‌داره.</summary>
        public const string ActionParameter = "action";
        public const string ActionHeader = "X-Gateway-Action";

        /// <summary>
        /// هندلرها فقط POST قبول می‌کنن و متد واقعی رو از route پیدا می‌کنن؛ این هدر فقط وقتی لازمه که یک مسیر چند متد داشته باشه.
        /// </summary>
        public const string MethodOverrideHeader = "X-HTTP-Method-Override";

        /// <summary>هندلر → کنترلرهایی که پشتش هستن (نام کنترلر = اولین بخش مسیر بعد از /api/).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Handlers =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // ورود، توکن، فراموشی رمز، مدیریت کاربران و دسترسی‌ها
                ["auth-handler"] = new[] { "Auth" },
                // درخواست‌ها، کارتابل، شرکت‌ها، گزارش‌ها و نوبت‌دهی ارزیابی
                ["ehraz-handler"] = new[] { "Request", "Cartables", "Company", "Reports", "EvalAppointment" },
                // سرویس‌های عملیاتی و خارجی (شاهکار، پیامک، bsr، متن سند و قبض انبار)
                ["service-handler"] = new[] { "Service" },
            };

        /// <summary>هندلری که کنترلر داده‌شده پشتش قرار داره، یا null.</summary>
        public static string? FindHandlerForController(string controller)
        {
            foreach (var (handler, controllers) in Handlers)
            {
                if (controllers.Contains(controller, StringComparer.OrdinalIgnoreCase))
                    return handler;
            }
            return null;
        }

        /// <summary>
        /// مسیر action باید نسبی، بدون ".." و فقط شامل کاراکترهای مجاز باشه، و کنترلرش متعلق به همین هندلر باشه.
        /// </summary>
        public static bool IsActionAllowed(string handler, string action)
        {
            if (string.IsNullOrWhiteSpace(action) || !Handlers.TryGetValue(handler, out var controllers))
                return false;

            if (action.Contains("..") || action.Contains("//") || action.StartsWith('/'))
                return false;

            foreach (var ch in action)
            {
                if (!(char.IsAsciiLetterOrDigit(ch) || ch == '/' || ch == '-' || ch == '_' || ch == '.'))
                    return false;
            }

            var controller = action.Split('/')[0];
            return controllers.Contains(controller, StringComparer.OrdinalIgnoreCase);
        }
    }
}
