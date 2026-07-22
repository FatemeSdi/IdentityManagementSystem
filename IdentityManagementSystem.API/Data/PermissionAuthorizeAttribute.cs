using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdentityManagementSystem.API.Data;
using System.Linq;

namespace IdentityManagementSystem
{
    public class PermissionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _permission;

        public PermissionAuthorizeAttribute(string permission)
        {
            _permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var dbContext = context.HttpContext.RequestServices.GetService(typeof(IdentityManagementSystemContext)) as IdentityManagementSystemContext;

            // گرفتن UserId از سشن یا کوکی (اینجا باید بعد از لاگین ست بشه)
            var userIdString = context.HttpContext.User?.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out long userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // چک کردن پرمیشن در دیتابیس
            var hasPermission = dbContext.UserAccesses
                .Any(ua => ua.UserId == userId && ua.Permission == _permission);

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
