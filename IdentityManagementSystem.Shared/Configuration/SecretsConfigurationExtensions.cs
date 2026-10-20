using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace IdentityManagementSystem.Shared.Configuration
{
    public static class SecretsConfigurationExtensions
    {
        public const string SecretsFileName = "appsettings.Secrets.json";

        /// <summary>
        /// رمزها و کلیدها (JWT، رمزنگاری، شاهکار، bsr، ApiKey، حساب سرویس پورتال) تو appsettings.json گیت نیستن؛
        /// از فایل appsettings.Secrets.json (که تو .gitignore هست) خونده می‌شن. نمونه‌ی ساختار: appsettings.Secrets.example.json.
        /// فایل درست بعد از appsettings.{Environment}.json اضافه می‌شه، پس متغیرهای محیطی و آرگومان‌های خط فرمان
        /// (مثلاً Jwt__Key روی سرور) همچنان می‌تونن مقدارش رو override کنن.
        /// </summary>
        public static ConfigurationManager AddSecretsFile(this ConfigurationManager configuration)
        {
            var sources = configuration.Sources;
            var lastAppSettingsIndex = -1;
            for (var i = 0; i < sources.Count; i++)
            {
                if (sources[i] is JsonConfigurationSource json &&
                    json.Path != null &&
                    json.Path.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase))
                {
                    lastAppSettingsIndex = i;
                }
            }

            var secretsSource = new JsonConfigurationSource
            {
                Path = SecretsFileName,
                Optional = true,
                ReloadOnChange = true,
                // همون content root ای که appsettings.json ازش خونده می‌شه
                FileProvider = configuration.GetFileProvider()
            };

            sources.Insert(lastAppSettingsIndex + 1, secretsSource);
            return configuration;
        }
    }
}
