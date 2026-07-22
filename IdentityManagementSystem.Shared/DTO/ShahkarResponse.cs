using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class ShahkarResponse
    {
        [JsonPropertyName("isSuccessful")]
        public bool IsSuccessful { get; set; }

        [JsonPropertyName("error")]
        public int Error { get; set; }

        [JsonPropertyName("errorDescription")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("responseText")]
        public string? ResponseText { get; set; }

        [JsonPropertyName("responseStatusCode")]
        public int ResponseStatusCode { get; set; }

        [JsonPropertyName("usedQuotaStats")]
        public UsedQuotaStats? UsedQuotaStats { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }
}
