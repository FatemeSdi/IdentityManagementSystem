using Microsoft.AspNetCore.Http;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityManagementSystem.API.Services.Logging
{
    public class UserActionLogger
    {
        private readonly IdentityManagementSystemContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserActionLogger(IdentityManagementSystemContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(long userId, string action, string actionResult, string logLevel = "Info")
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

                var log = new UserLog
                {
                    UserId = userId,
                    Action = action,
                    ActionResult = actionResult,
                    ActionTime = DateTime.UtcNow,
                    IpAddress = ip,
                    UserAgent = userAgent,
                    LogLevel = logLevel
                };

                _context.UserLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error writing log: {ex.Message}");
            }
        }

        // متدهای کمکی برای استفاده راحت‌تر
        public Task Info(long userId, string action, string result)
            => LogAsync(userId, action, result, "Info");

        public Task Warning(long userId, string action, string result)
            => LogAsync(userId, action, result, "Warning");

        public Task Error(long userId, string action, string result)
            => LogAsync(userId, action, result, "Error");

        internal async Task Error(ClaimsPrincipal user, string v, string message)
        {
            throw new NotImplementedException();
        }
    }
}
