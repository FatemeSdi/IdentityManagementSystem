using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class ResultWrapper
    {
        public DataWrapper? Data { get; set; }
        public StatusWrapper? Status { get; set; }
    }
}
