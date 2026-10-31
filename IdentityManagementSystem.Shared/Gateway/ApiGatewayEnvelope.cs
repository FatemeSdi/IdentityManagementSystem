using System.Text.Json;

namespace IdentityManagementSystem.Shared.Gateway
{
    /// <summary>
    /// بدنه‌ی ثابت درخواست به هندلرها. Gateway سازمان (Pomix) هر سرویس رو با یک قرارداد ثابت ثبت می‌کنه و فقط ورودی‌های
    /// ثبت‌شده رو عبور می‌ده؛ query string و هدرهای سفارشی ممکنه نرسن. برای همین همه‌ی مشخصات درخواست اصلی توی body میاد:
    ///   POST /api/ehraz-handler
    ///   { "action": "Request/Track", "method": "GET", "query": { "trackingCode": "X" }, "body": null }
    /// action — مسیر سرویس مقصد بعد از /api/ (الزامی)
    /// method — متد اصلی سرویس (اختیاری؛ نبودنش = API از route پیدا می‌کنه)
    /// query  — پارامترهای query سرویس؛ مقدار هر کلید رشته/عدد/بولین یا آرایه‌ای از اون‌ها (اختیاری)
    /// body   — بدنه‌ی JSON خودِ سرویس (اختیاری)
    /// </summary>
    public static class ApiGatewayEnvelope
    {
        public const string ActionField = "action";
        public const string MethodField = "method";
        public const string QueryField = "query";
        public const string BodyField = "body";

        public const string ContentType = "application/json";

        /// <summary>
        /// envelope رو می‌سازه. bodyJson باید JSON معتبر یا null باشه؛ query رشته‌ی query اصلی (با یا بدون ?) است.
        /// </summary>
        public static byte[] Build(string action, string method, string? query, string? bodyJson)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString(ActionField, action);
                writer.WriteString(MethodField, method);

                writer.WriteStartObject(QueryField);
                foreach (var group in ParseQuery(query).GroupBy(p => p.Key, StringComparer.Ordinal))
                {
                    var values = group.Select(p => p.Value).ToList();
                    if (values.Count == 1)
                    {
                        writer.WriteString(group.Key, values[0]);
                        continue;
                    }

                    writer.WriteStartArray(group.Key);
                    foreach (var value in values)
                        writer.WriteStringValue(value);
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();

                writer.WritePropertyName(BodyField);
                if (string.IsNullOrWhiteSpace(bodyJson))
                    writer.WriteNullValue();
                else
                    writer.WriteRawValue(bodyJson); // اعتبار JSON رو هم چک می‌کنه

                writer.WriteEndObject();
            }

            return stream.ToArray();
        }

        private static IEnumerable<KeyValuePair<string, string>> ParseQuery(string? query)
        {
            if (string.IsNullOrEmpty(query))
                yield break;

            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var index = part.IndexOf('=');
                var key = index < 0 ? part : part[..index];
                var value = index < 0 ? string.Empty : part[(index + 1)..];
                yield return new(Decode(key), Decode(value));
            }
        }

        private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));
    }
}
