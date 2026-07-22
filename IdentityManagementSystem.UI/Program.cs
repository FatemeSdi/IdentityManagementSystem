using DNTCaptcha.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using IdentityManagementSystem.UI.Filters;

var builder = WebApplication.CreateBuilder(args);

// ================= MVC =================
builder.Services.AddControllersWithViews();

// ================= Cookie Auth =================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/LoginPage";
        options.AccessDeniedPath = "/Error/AccessDenied";
        options.ReturnUrlParameter = "returnUrl";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// ================= API Settings =================
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

builder.Services.AddHttpClient("PomixApiPublic", client =>
{
    client.BaseAddress = new Uri($"{apiUrl}/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});

// ================= Captcha =================
builder.Services.AddDNTCaptcha(options =>
{
    options.UseCookieStorageProvider()
           .ShowThousandsSeparators(false)
           .WithEncryptionKey("YourEncryptionKey")
           .AbsoluteExpiration(minutes: 7);
});

// ================= Session =================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ================= Filters =================
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new NoCacheFilterAttribute());
});

// ================= Build =================
var app = builder.Build();

// ================= Pipeline =================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
});

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();


// ================= FIXED AUTH MIDDLEWARE =================
// ================= FIXED AUTH MIDDLEWARE =================
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";

    // مسیرهای آزاد
    if (path.StartsWith("/home/loginpage") ||
        path.StartsWith("/api") ||
        path.StartsWith("/swagger") ||
        path.Contains("captcha") ||
        path.StartsWith("/css") ||
        path.StartsWith("/js") ||
        path.StartsWith("/lib") ||
        path.StartsWith("/assets"))
    {
        await next();
        return;
    }

    var token = context.Session.GetString("JwtToken");

    if (string.IsNullOrEmpty(token))
    {
        var accept = context.Request.Headers["Accept"].ToString().ToLower();
        if (accept.Contains("text/html"))
        {
            context.Response.Redirect("/Home/LoginPage?returnUrl=" + context.Request.Path);
            return;
        }

        context.Response.StatusCode = 401;
        return;
    }

    await next();
});

// ================= Routing =================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cartable}/{action=Index}/{id?}");

app.Run();