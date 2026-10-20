using Microsoft.AspNetCore.Cors.Infrastructure;

namespace IdentityManagementSystem.API.Gateway
{
    /// <summary>
    /// CORS از بخش Cors در appsettings.json، در هر درخواست خونده می‌شه (پس بدون restart قابل تغییره):
    ///   Cors:Enabled        — false (پیش‌فرض) = هیچ سایتی تو مرورگر اجازه‌ی صدا زدن API رو نداره
    ///   Cors:AllowedOrigins — آدرس‌های مجاز، جدا با کاما (مثلاً "https://localhost:7031, http://localhost:5031")
    /// Enabled = true با AllowedOrigins خالی هم یعنی هیچ سایتی مجاز نیست؛ هیچ‌وقت «همه مجاز» نمی‌شه.
    /// UI و PublicPortal سرور-به-سرور به API وصل می‌شن و CORS روی اون ارتباط اثری نداره؛ این فقط برای دسترسی مستقیم مرورگره.
    /// </summary>
    public class ConfigurableCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly IConfiguration _configuration;

        public ConfigurableCorsPolicyProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            var section = _configuration.GetSection("Cors");
            if (!section.GetValue<bool>("Enabled"))
                return Task.FromResult<CorsPolicy?>(null);

            // رشته‌ی جدا با کاما (نه آرایه‌ی JSON) تا بین appsettings.json و appsettings.{Environment}.json اندیس‌به‌اندیس ادغام نشه
            var origins = (section["AllowedOrigins"] ?? string.Empty)
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(o => o.TrimEnd('/'))
                .ToArray();
            if (origins.Length == 0)
                return Task.FromResult<CorsPolicy?>(null);

            var policy = new CorsPolicyBuilder()
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .Build();
            return Task.FromResult<CorsPolicy?>(policy);
        }
    }
}
