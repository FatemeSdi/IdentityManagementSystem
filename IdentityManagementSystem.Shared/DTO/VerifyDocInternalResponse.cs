using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class VerifyDocInternalResponse
    {
        public VerifyDocResultWrapper? Result { get; set; }
        public StatusWrapper? Status { get; set; }
    }
}
