using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class VerifyDocResponse
    {
        public bool IsSuccessful { get; set; }
        public string? ResponseText { get; set; }
        public List<PersonInQuery>? PersonsInQuery { get; set; }
        public bool ExistDoc { get; set; }
        public bool IsNationalIdInLawyers { get; set; }
        public bool IsNationalIdInResponse { get; set; }

    }
}
