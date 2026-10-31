using IdentityManagementSystem.Shared.Gateway;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IdentityManagementSystem.API.Gateway
{
    /// <summary>
    /// Swagger فقط سه هندلر (auth-handler / ehraz-handler / service-handler) رو نشون می‌ده، نه مسیرهای مستقیم کنترلرها.
    /// هر هندلر فقط یک operation با POST و body ثابت (ApiGatewayEnvelope) داره (همه‌ی سرویس‌های GET/PUT/DELETE/POST پشتش هستن)؛
    /// توضیحاتش فهرست سرویس‌ها با متد، پارامترهای query و نام مدل body هر کدوم رو داره، پس "Try it out" مستقیم از هندلر رد می‌شه.
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
                var queryNames = (a.Operation.Parameters ?? new List<OpenApiParameter>())
                    .Where(p => p.In == ParameterLocation.Query)
                    .Select(p => $"`{p.Name}`")
                    .ToList();
                var summary = string.IsNullOrWhiteSpace(a.Operation.Summary) ? "" : $" — {a.Operation.Summary}";
                return $"- `{a.Action}` — method: {a.Method.ToString().ToUpperInvariant()}"
                    + (queryNames.Count > 0 ? $" (query: {string.Join("، ", queryNames)})" : "")
                    + (body != null ? $" (body: `{body}`)" : "")
                    + summary;
            }));

            // قرارداد ثابت هندلر: فقط POST با یک body JSON (ApiGatewayEnvelope)؛ بدون پارامتر query و هدر سفارشی
            var first = actions[0];
            var operation = new OpenApiOperation
            {
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = handler } },
                OperationId = handler,
                Summary = $"POST /api/{handler}",
                Description = "همه‌ی سرویس‌ها با POST و یک body ثابت صدا زده می‌شن: سرویس مقصد در `action`، متد اصلی در `method`، "
                    + "پارامترهای query سرویس در `query` و بدنه‌ی خود سرویس در `body`. "
                    + "بخش‌های `{...}` رو با مقدار واقعی جایگزین کنید، مثلاً `Auth/UpdateUser/12`:\n\n" + actionList,
                Parameters = new List<OpenApiParameter>(),
                RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        [ApiGatewayEnvelope.ContentType] = new OpenApiMediaType
                        {
                            Schema = EnvelopeSchema(),
                            Example = new Microsoft.OpenApi.Any.OpenApiObject
                            {
                                [ApiGatewayEnvelope.ActionField] = new Microsoft.OpenApi.Any.OpenApiString(first.Action),
                                [ApiGatewayEnvelope.MethodField] = new Microsoft.OpenApi.Any.OpenApiString(first.Method.ToString().ToUpperInvariant()),
                                [ApiGatewayEnvelope.QueryField] = new Microsoft.OpenApi.Any.OpenApiObject(),
                                [ApiGatewayEnvelope.BodyField] = first.Operation.RequestBody != null
                                    ? new Microsoft.OpenApi.Any.OpenApiObject()
                                    : new Microsoft.OpenApi.Any.OpenApiNull()
                            }
                        }
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "پاسخ سرویس مقصد" },
                    ["400"] = new OpenApiResponse { Description = "body هندلر نامعتبر، یا method با تعریف سرویس جور نیست" },
                    ["401"] = new OpenApiResponse { Description = "توکن نامعتبر یا ارسال نشده" },
                    ["404"] = new OpenApiResponse { Description = "سرویس پشت این هندلر تعریف نشده" }
                }
            };

            return operation;
        }

        private static OpenApiSchema EnvelopeSchema() => new()
        {
            Type = "object",
            Required = new HashSet<string> { ApiGatewayEnvelope.ActionField },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                [ApiGatewayEnvelope.ActionField] = new() { Type = "string", Description = "مسیر سرویس مقصد (همون مسیر قبلی بعد از /api/)" },
                [ApiGatewayEnvelope.MethodField] = new()
                {
                    Type = "string",
                    Nullable = true,
                    Description = "متد اصلی سرویس؛ اگه نیاد API از تعریف سرویس پیدا می‌کنه",
                    Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                    {
                        new Microsoft.OpenApi.Any.OpenApiString("GET"),
                        new Microsoft.OpenApi.Any.OpenApiString("POST"),
                        new Microsoft.OpenApi.Any.OpenApiString("PUT"),
                        new Microsoft.OpenApi.Any.OpenApiString("DELETE")
                    }
                },
                [ApiGatewayEnvelope.QueryField] = new()
                {
                    Type = "object",
                    Nullable = true,
                    Description = "پارامترهای query سرویس (فهرست هر سرویس در توضیحات بالا)؛ مقدار: رشته، عدد، بولین یا آرایه",
                    AdditionalPropertiesAllowed = true
                },
                [ApiGatewayEnvelope.BodyField] = new()
                {
                    Type = "object",
                    Nullable = true,
                    Description = "بدنه‌ی JSON خود سرویس (مدل هر سرویس در توضیحات بالا)"
                }
            }
        };

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
