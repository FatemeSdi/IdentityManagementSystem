using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using IdentityManagementSystem.API.Controllers;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Services;
using IdentityManagementSystem.API.Services.Logging;
using IdentityManagementSystem.Shared.Configuration;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// رمزها و کلیدها از appsettings.Secrets.json (خارج از گیت) خونده می‌شن — نمونه: appsettings.Secrets.example.json
builder.Configuration.AddSecretsFile();
// اگه appsettings*.json موقع اجرا خراب ذخیره بشه، همه‌ی درخواست‌ها تا اصلاحش با 503 رد می‌شن (نه اینکه با تنظیمات خالی ادامه بده)
IdentityManagementSystem.Shared.Configuration.ConfigurationGuardExtensions.AddConfigurationGuard(builder);
// آدرس/پورت (بخش Kestrel) فقط موقع بالا اومدن خونده بشه؛ وگرنه اگه appsettings.json خراب ذخیره بشه Kestrel
// پورت رو کلاً می‌بنده و به‌جای پیام 503 واضح، API بی‌جواب می‌شه. تغییر پورت هم در هر حال restart می‌خواد.
builder.WebHost.ConfigureKestrel((context, options) =>
    options.Configure(context.Configuration.GetSection("Kestrel"), reloadOnChange: false));

// --- DbContext ---
builder.Services.AddDbContext<IdentityManagementSystemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Services ---
builder.Services.AddControllers();
// سرویس‌های خارجی (شاهکار / bsr-*) نباید از پراکسی سیستم (HTTP_PROXY/HTTPS_PROXY محیطی، مثلاً از یه VPN محلی)
// عبور کنن — این پراکسی هندشیک TLS به core.pomix.pmo.ir رو می‌شکنه، در حالی که اتصال مستقیم سالمه.
// تایم‌اوت کوتاه‌تر از ۱۰۰ ثانیه‌ی پیش‌فرض: وقتی سرویس خارجی جواب نمی‌ده (مثلاً VPN/فیلتر هندشیک TLS رو قطع کرده)
// API باید زودتر از UI/PublicPortal (که خودشون ۱۰۰ ثانیه صبر می‌کنن) خطای واضح برگردونه، نه اینکه کاربر بی‌جواب بمونه.
var externalServicesTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("ExternalServices:TimeoutSeconds", 30));
builder.Services.AddHttpClient(string.Empty)
    .ConfigureHttpClient(client => client.Timeout = externalServicesTimeout)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseProxy = false });
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TokenService, TokenService>();
builder.Services.Configure<ShahkarServiceOptions>(builder.Configuration.GetSection("Shahkar"));
builder.Services.Configure<IdentityManagementSystem.API.Controllers.BsrServiceOptions>(builder.Configuration.GetSection("Bsr"));
builder.Services.AddScoped<IdentityManagementSystem.API.Helpers.EncryptionHelper>();
builder.Services.AddHttpClient<IdentityManagementSystem.API.Services.Sms.ISmsService, IdentityManagementSystem.API.Services.Sms.SmsService>();

// --- Authentication & Authorization ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException($"Jwt:Key تنظیم نشده — فایل {SecretsConfigurationExtensions.SecretsFileName} رو کنار برنامه بذارید (نمونه: appsettings.Secrets.example.json).");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessServices", policy =>
        policy.RequireClaim("Permission", "CanAccessServices"));


    options.AddPolicy("CanValidateRequest", policy =>
        policy.RequireClaim("Permission", "CanValidateRequest"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserActionLogger>();

// --- Swagger ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ServicePomixPMO API", Version = "v1" });
    // فقط سه هندلر gateway نمایش داده می‌شن، نه مسیرهای مستقیم کنترلرها
    c.DocumentFilter<IdentityManagementSystem.API.Gateway.GatewayHandlersDocumentFilter>();
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "لطفاً توکن JWT را وارد کنید: Bearer {token}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "کلید دسترسی (ApiKey در appsettings.Secrets.json)",
        Name = IdentityManagementSystem.API.Gateway.ApiKeyMiddleware.HeaderName,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- CORS ---
// روشن/خاموش و آدرس‌های مجاز از بخش Cors در appsettings.json، در هر درخواست خونده می‌شن (بدون restart).
// پیش‌فرض خاموشه؛ UI/PublicPortal سرور-به-سرور وصل می‌شن و به CORS نیاز ندارن.
builder.Services.AddCors();
builder.Services.AddSingleton<Microsoft.AspNetCore.Cors.Infrastructure.ICorsPolicyProvider,
    IdentityManagementSystem.API.Gateway.ConfigurableCorsPolicyProvider>();

var app = builder.Build();

// باید اولین middleware باشه — جلوی اجرای درخواست با تنظیمات خالی/خراب رو می‌گیره
app.UseConfigurationGuard();

// --- Middleware ---
// UI/PublicPortal سرور-به-سرور به API وصل می‌شن؛ بدون این، UserLog فقط IP/UserAgent خودِ سرور UI رو
// می‌بینه نه کاربر واقعی رو. UI با ClientContextForwardingHandler هدر X-Forwarded-For رو ست می‌کنه؛
// اینجا با اعتماد به همه‌ی پراکسی‌ها (چون API فقط از شبکه‌ی داخلی/سرورهای خودمون صدا زده می‌شه) قبولش می‌کنیم.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
// IP واقعی اتصال (قبل از اعمال X-Forwarded-For که قابل جعله) برای تشخیص درخواست‌هایی که از Gateway سازمان (Pomix) میان
app.Use((context, next) =>
{
    context.Items[IdentityManagementSystem.API.Gateway.ApiGatewayMiddleware.ConnectionIpItemKey] = context.Connection.RemoteIpAddress;
    return next();
});
app.UseForwardedHeaders(forwardedHeadersOptions);

// هندلرهای gateway (auth-handler / ehraz-handler / service-handler) — باید قبل از UseRouting باشه
// تا /api/{handler}?action=... به مسیر اصلی کنترلر بازنویسی بشه. Gateway:AllowedModes مشخص می‌کنه کدوم مسیرها (Pomix / DirectGateway / DirectApi) باز باشن.
// قبلش X-API-Key چک می‌شه تا فقط UI/PublicPortal خودمون بتونن /api/* رو صدا بزنن.
// CORS باید قبل از چک X-API-Key باشه: درخواست preflight (OPTIONS) مرورگر هیچ‌وقت هدر X-API-Key نداره.
app.UseCors();
app.UseMiddleware<IdentityManagementSystem.API.Gateway.ApiKeyMiddleware>();
app.UseMiddleware<IdentityManagementSystem.API.Gateway.ApiGatewayMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServicePomixPMO API V1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
