using IdentityManagementSystem.Shared.Configuration;
using IdentityManagementSystem.PublicPortal.Filters;
using IdentityManagementSystem.Shared.Http;

var builder = WebApplication.CreateBuilder(args);
// ApiKey و رمز حساب سرویس پورتال از appsettings.Secrets.json (خارج از گیت) خونده می‌شن
IdentityManagementSystem.Shared.Configuration.SecretsConfigurationExtensions.AddSecretsFile(builder.Configuration);
// اگه appsettings*.json موقع اجرا خراب ذخیره بشه، همه‌ی درخواست‌ها تا اصلاحش با 503 رد می‌شن (نه اینکه با تنظیمات خالی ادامه بده)
IdentityManagementSystem.Shared.Configuration.ConfigurationGuardExtensions.AddConfigurationGuard(builder);

var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    builder.WebHost.UseUrls($"http://localhost:{portEnv}");
}

// ================= MVC =================
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new NoCacheFilterAttribute());
});

// ================= Antiforgery =================
// اجازه می‌ده توکن CSRF از طریق هدر هم (نه فقط فیلد فرم) ارسال بشه —
// چون فرم ثبت درخواست با fetch/$.ajax و JSON body کار می‌کنه.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// ================= API Settings =================
// این پروژه فقط با لایه‌ی داخلی API (سرور به سرور) حرف می‌زنه — مرورگر کاربر هیچوقت مستقیم به API دسترسی نداره.
var apiUrl = builder.Configuration["ApiSettings:URL"];
var apiKey = builder.Configuration["ApiSettings:ApiKey"];

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ClientContextForwardingHandler>();
// اگه ApiSettings:Gateway:Enabled روشن باشه، درخواست‌ها به‌جای مسیر مستقیم از هندلرهای gateway (auth/ehraz/service) رد می‌شن
builder.Services.AddTransient<IdentityManagementSystem.Shared.Gateway.ApiGatewayRoutingHandler>();

builder.Services.AddHttpClient("PomixApi", client =>
{
    client.BaseAddress = new Uri($"{apiUrl}/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
})
.AddHttpMessageHandler<ClientContextForwardingHandler>()
.AddHttpMessageHandler<IdentityManagementSystem.Shared.Gateway.ApiGatewayRoutingHandler>();

// ================= Session =================
// وضعیت تایید OTP (قبل از ثبت درخواست) اینجا نگه‌داری می‌شه.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ================= Build =================
var app = builder.Build();

// باید اولین middleware باشه — جلوی اجرای درخواست با تنظیمات خالی/خراب رو می‌گیره
app.UseConfigurationGuard();

// ================= Pipeline =================
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cartable}/{action=ClientIndex}/{id?}");

app.Run();
