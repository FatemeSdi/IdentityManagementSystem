using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using IdentityManagementSystem.API.Controllers;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Services;
using IdentityManagementSystem.API.Services.Logging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- DbContext ---
builder.Services.AddDbContext<IdentityManagementSystemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Services ---
builder.Services.AddControllers();
// سرویس‌های خارجی (شاهکار / bsr-*) نباید از پراکسی سیستم (HTTP_PROXY/HTTPS_PROXY محیطی، مثلاً از یه VPN محلی)
// عبور کنن — این پراکسی هندشیک TLS به core.pomix.pmo.ir رو می‌شکنه، در حالی که اتصال مستقیم سالمه.
builder.Services.AddHttpClient(string.Empty)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseProxy = false });
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TokenService, TokenService>();
builder.Services.Configure<ShahkarServiceOptions>(builder.Configuration.GetSection("Shahkar"));
builder.Services.Configure<IdentityManagementSystem.API.Controllers.BsrServiceOptions>(builder.Configuration.GetSection("Bsr"));
builder.Services.AddScoped<IdentityManagementSystem.API.Helpers.EncryptionHelper>();
builder.Services.AddHttpClient<IdentityManagementSystem.API.Services.Sms.ISmsService, IdentityManagementSystem.API.Services.Sms.SmsService>();

// --- Authentication & Authorization ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

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
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "لطفاً توکن JWT را وارد کنید: Bearer {token}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
        }
    });
});

// --- CORS ---
// آدرس‌های مجاز از appsettings.json خونده می‌شن (بخش Cors:AllowedOrigins) تا بعد از publish
// روی هر سرور (UI متقاضی روی اینترنت، UI متصدی تو شبکه داخلی) بدون rebuild قابل تغییر باشن.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:7031", "https://localhost:7031" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// --- Middleware ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServicePomixPMO API V1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

//app.UseCors("AllowFrontend");
app.UseCors("AllowUI");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
