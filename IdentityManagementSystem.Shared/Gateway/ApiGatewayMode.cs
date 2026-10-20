namespace IdentityManagementSystem.Shared.Gateway
{
    /// <summary>
    /// مسیر ارتباط UI/PublicPortal با API:
    ///   Pomix         — از طریق Gateway سازمان (Pomix)؛ آدرس هر سه هندلر از ApiSettings:PomixUrls
    ///   DirectGateway — مستقیم به سرور API ولی فقط از سه هندلر (POST {ApiSettings:URL}/api/{handler}?action=...)
    ///   DirectApi     — مستقیم به همه‌ی اندپوینت‌های API ({ApiSettings:URL}/api/Auth/login و ...) — فقط برای عیب‌یابی تو شبکه‌ی داخلی
    /// سمت API، Gateway:AllowedModes مشخص می‌کنه کدوم حالت‌ها قبول بشن.
    /// </summary>
    public enum ApiGatewayMode
    {
        Pomix,
        DirectGateway,
        DirectApi
    }

    public static class ApiGatewayModes
    {
        public static ApiGatewayMode Parse(string? value, ApiGatewayMode defaultMode)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultMode;

            if (Enum.TryParse<ApiGatewayMode>(value.Trim(), ignoreCase: true, out var mode) && Enum.IsDefined(mode))
                return mode;

            throw new InvalidOperationException(
                $"مقدار '{value}' برای حالت ارتباط با API معتبر نیست؛ یکی از این‌ها باشد: {string.Join(" | ", Enum.GetNames<ApiGatewayMode>())}");
        }
    }
}
