using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class VerifyDocResultWrapper
    {
        public JsonElement Data { get; set; }
        public StatusWrapper? Status { get; set; }
    }
}
