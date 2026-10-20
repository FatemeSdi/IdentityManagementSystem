using IdentityManagementSystem.Shared.Gateway;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IdentityManagementSystem.API.Gateway
{
    /// <summary>
    /// Swagger فقط سه هندلر (auth-handler / ehraz-handler / service-handler) رو نشون می‌ده، نه مسیرهای مستقیم کنترلرها.
    /// هر هندلر فقط یک operation با POST داره (همه‌ی سرویس‌های GET/PUT/DELETE/POST پشتش هستن) که پارامتر action، پارامترهای query سرویس‌ها
    /// و نام مدل body هر سرویس رو (از همون مستند اصلی) داره، پس "Try it out" مستقیم از هندلر رد می‌شه.
    /// </summary>
    public class GatewayHandlersDocumentFilter : IDocumentFilter
    {
        private const string ApiPrefix = "/api/";

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var handlerPaths = new OpenApiPaths();

            foreach (var handler in ApiGatewayRoutes.Handlers.Keys)
            {
                var actions = swaggerDoc.Paths
                    .Where(p => p.Key.StartsWith(ApiPrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(p => (Action: p.Key[ApiPrefix.Length..], Item: p.Value))
                    .Where(p => string.Equals(ApiGatewayRoutes.FindHandlerForController(p.Action.Split('/')[0]), handler, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(p => p.Item.Operations.Select(o => (p.Action, Method: o.Key, Operation: o.Value)))
                    .ToList();

                if (actions.Count == 0)
                    continue;

                // هر هندلر فقط یک operation با POST داره؛ متد اصلی هر سرویس تو توضیحات میاد و API خودش اعمالش می‌کنه
                var pathItem = new OpenApiPathItem();
                pathItem.Operations[OperationType.Post] = BuildOperation(handler,
                    actions.OrderBy(a => a.Action, StringComparer.OrdinalIgnoreCase).ThenBy(a => a.Method).ToList());
                handlerPaths[ApiPrefix + handler] = pathItem;
            }

            swaggerDoc.Paths = handlerPaths;
        }

        private static OpenApiOperation BuildOperation(string handler, List<(string Action, OperationType Method, OpenApiOperation Operation)> actions)
        {
            var actionList = string.Join("\n", actions.Select(a =>
            {
                var body = GetBodySchemaName(a.Operation);
                var summary = string.IsNullOrWhiteSpace(a.Operation.Summary) ? "" : $" — {a.Operation.Summary}";
                return $"- `{a.Action}` — متد اصلی: {a.Method.ToString().ToUpperInvariant()}{(body != null ? $" (body: `{body}`)" : "")}{summary}";
            }));

            var operation = new OpenApiOperation
            {
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = handler } },
                OperationId = handler,
                Summary = $"POST /api/{handler}?action=...",
                Description = "همه‌ی سرویس‌ها با POST صدا زده می‌شن؛ API متد اصلی هر سرویس رو خودش اعمال می‌کنه. "
                    + "بخش‌های `{...}` رو با مقدار واقعی جایگزین کنید، مثلاً `Auth/UpdateUser/12`:\n\n" + actionList,
                Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter
                    {
                        Name = ApiGatewayRoutes.ActionParameter,
                        In = ParameterLocation.Query,
                        Required = true,
                        Description = "مسیر سرویس مقصد (همون مسیر قبلی بعد از /api/)",
                        Schema = new OpenApiSchema { Type = "string" },
                        Example = new Microsoft.OpenApi.Any.OpenApiString(actions[0].Action)
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "پاسخ سرویس مقصد" },
                    ["401"] = new OpenApiResponse { Description = "توکن نامعتبر یا ارسال نشده" },
                    ["404"] = new OpenApiResponse { Description = "سرویس پشت این هندلر تعریف نشده" }
                }
            };

            // پارامترهای query سرویس‌ها (مثل trackingCode یا page) — همه اختیاری، چون هر کدوم مال یک سرویس خاصه
            foreach (var group in actions
                .SelectMany(a => (a.Operation.Parameters ?? new List<OpenApiParameter>())
                    .Where(p => p.In == ParameterLocation.Query)
                    .Select(p => (a.Action, Parameter: p)))
                .GroupBy(p => p.Parameter.Name, StringComparer.OrdinalIgnoreCase))
            {
                var first = group.First().Parameter;
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = first.Name,
                    In = ParameterLocation.Query,
                    Required = false,
                    Schema = first.Schema,
                    Description = "مخصوص: " + string.Join("، ", group.Select(p => p.Action).Distinct())
                });
            }

            if (actions.Any(a => a.Operation.RequestBody != null))
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = false,
                    Description = "بدنه‌ی JSON همون سرویسی که در action انتخاب شده (مدل هر سرویس در توضیحات بالا آمده)",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = "object" } }
                    }
                };
            }

            return operation;
        }

        private static string? GetBodySchemaName(OpenApiOperation operation)
        {
            var content = operation.RequestBody?.Content;
            if (content == null || content.Count == 0)
                return null;

            var schema = content.TryGetValue("application/json", out var json) ? json.Schema : content.Values.First().Schema;
            return schema?.Reference?.Id ?? schema?.Type;
        }
    }
}
