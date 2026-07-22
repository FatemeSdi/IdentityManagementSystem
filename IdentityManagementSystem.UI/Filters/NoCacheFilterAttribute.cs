using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityManagementSystem.UI.Filters
{
    public class NoCacheFilterAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Path.StartsWithSegments("/Home/LoginPage"))
            {
                context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.HttpContext.Response.Headers["Pragma"] = "no-cache";
                context.HttpContext.Response.Headers["Expires"] = "0";
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}