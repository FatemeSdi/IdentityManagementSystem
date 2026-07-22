using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class DataWrapper
    {
        public string? Result { get; set; }
        public string? RequestId { get; set; }
        public int Response { get; set; }
        public string? Comment { get; set; }
        public string? Id { get; set; }
    }

}
