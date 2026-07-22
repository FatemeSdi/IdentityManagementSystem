using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class ShahkarServiceOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public Credential Credential { get; set; } = new Credential();
    }
}
