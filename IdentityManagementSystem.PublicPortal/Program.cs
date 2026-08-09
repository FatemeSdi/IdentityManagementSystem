using IdentityManagementSystem.PublicPortal.Filters;

var builder = WebApplication.CreateBuilder(args);

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
});

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

// ================= Pipeline =================
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cartable}/{action=ClientIndex}/{id?}");

app.Run();
