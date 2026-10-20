using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace IdentityManagementSystem.Shared.Configuration
{
    /// <summary>
    /// اگه یکی از فایل‌های تنظیمات (appsettings*.json) موقع اجرا خراب ذخیره بشه، .NET همه‌ی مقدارهای اون فایل رو
    /// پاک می‌کنه و برنامه با تنظیمات خالی ادامه می‌ده (مثلاً Gateway:AllowedModes خالی = همه‌ی حالت‌ها باز).
    /// این گارد تا وقتی فایل درست نشده، همه‌ی درخواست‌ها رو با 503 و پیام واضح رد می‌کنه و جزئیات رو تو لاگ می‌نویسه؛
    /// بعد از اصلاح فایل، خودکار (بدون restart) برمی‌گرده. خرابی فایل موقع بالا اومدن برنامه هم مثل قبل جلوی اجرا رو می‌گیره.
    /// </summary>
    public sealed class ConfigurationGuard
    {
        private readonly ConcurrentDictionary<string, string> _brokenFiles = new(StringComparer.OrdinalIgnoreCase);
        private ILogger? _logger;

        public bool IsBroken => !_brokenFiles.IsEmpty;

        internal void SetLogger(ILogger logger) => _logger = logger;

        internal void MarkBroken(string file, string error)
        {
            if (_brokenFiles.TryAdd(file, error) || _brokenFiles[file] != error)
            {
                _brokenFiles[file] = error;
                _logger?.LogCritical("فایل تنظیمات {File} خراب است و همه‌ی درخواست‌ها تا اصلاح آن با 503 رد می‌شوند: {Error}", file, error);
            }
        }

        internal void MarkHealthy(string file)
        {
            if (_brokenFiles.TryRemove(file, out _))
                _logger?.LogWarning("فایل تنظیمات {File} اصلاح شد؛ برنامه دوباره درخواست‌ها را می‌پذیرد.", file);
        }
    }

    public static class ConfigurationGuardExtensions
    {
        // همون تنظیماتی که خود .NET برای خوندن appsettings.json استفاده می‌کنه
        private static readonly JsonDocumentOptions JsonOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>بعد از همه‌ی AddJsonFile ها (از جمله AddSecretsFile) صدا زده بشه.</summary>
        public static WebApplicationBuilder AddConfigurationGuard(this WebApplicationBuilder builder)
        {
            var guard = new ConfigurationGuard();
            var sources = builder.Configuration.Sources
                .OfType<FileConfigurationSource>()
                .Where(s => s.ReloadOnChange && !string.IsNullOrEmpty(s.Path))
                .ToList();

            foreach (var source in sources)
            {
                // فقط روی reload صدا زده می‌شه (بارگذاری اول قبلاً انجام شده و اگه خراب بود برنامه بالا نیومده)
                source.OnLoadException = context =>
                {
                    guard.MarkBroken(source.Path!, context.Exception.InnerException?.Message ?? context.Exception.Message);
                    context.Ignore = true;
                };
            }

            // بعد از هر reload دوباره چک می‌کنیم تا وقتی فایل اصلاح شد، حالت خرابی برداشته بشه
            IConfiguration configuration = builder.Configuration;
            ChangeToken.OnChange(configuration.GetReloadToken, () => Revalidate(guard, sources));

            builder.Services.AddSingleton(guard);
            return builder;
        }

        /// <summary>اولین middleware باشه تا هیچ درخواستی با تنظیمات خالی اجرا نشه.</summary>
        public static IApplicationBuilder UseConfigurationGuard(this IApplicationBuilder app)
        {
            var guard = app.ApplicationServices.GetRequiredService<ConfigurationGuard>();
            guard.SetLogger(app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("ConfigurationGuard"));

            return app.Use(async (context, next) =>
            {
                if (!guard.IsBroken)
                {
                    await next(context);
                    return;
                }

                // جزئیات (اسم فایل و خطا) فقط تو لاگه؛ به کاربر فقط اعلام می‌شه که تنظیمات خرابه
                const string message = "سامانه به‌دلیل خطا در فایل تنظیمات (appsettings) موقتاً در دسترس نیست. لطفاً به پشتیبانی اطلاع دهید.";
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers.RetryAfter = "60";

                var acceptsHtml = context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
                if (acceptsHtml)
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(
                        "<!doctype html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\"><title>خطای تنظیمات</title></head>" +
                        "<body style=\"font-family:Tahoma,sans-serif;padding:3rem;text-align:center\"><h2>خطای تنظیمات سامانه</h2>" +
                        $"<p>{message}</p></body></html>");
                }
                else
                {
                    await context.Response.WriteAsJsonAsync(new { success = false, message });
                }
            });
        }

        private static void Revalidate(ConfigurationGuard guard, IEnumerable<FileConfigurationSource> sources)
        {
            foreach (var source in sources)
            {
                var file = source.FileProvider?.GetFileInfo(source.Path!);
                if (file == null || !file.Exists)
                {
                    guard.MarkHealthy(source.Path!);
                    continue;
                }

                try
                {
                    using var stream = file.CreateReadStream();
                    using var _ = JsonDocument.Parse(stream, JsonOptions);
                    guard.MarkHealthy(source.Path!);
                }
                catch (JsonException ex)
                {
                    guard.MarkBroken(source.Path!, ex.Message);
                }
                catch (IOException)
                {
                    // فایل وسط ذخیره شدنه؛ reload بعدی دوباره چک می‌کنه
                }
            }
        }
    }
}
