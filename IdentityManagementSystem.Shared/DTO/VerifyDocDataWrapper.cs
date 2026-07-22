using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class VerifyDocDataWrapper
    {
        public List<object>? RegCases { get; set; }
        public List<object>? BaseDocuments { get; set; }
        public List<object>? FollowerDocuments { get; set; }
        public bool Succseed { get; set; }
        public string? NationalRegisterNo { get; set; }
        public string? DocType { get; set; }
        public string? DocType_code { get; set; }
        public bool HasPermission { get; set; }
        public bool ExistDoc { get; set; }
        public string? Desc { get; set; }
        public string? ScriptoriumName { get; set; }
        public string? SignGetterTitle { get; set; }
        public string? SignSubject { get; set; }
        public string? DocDate { get; set; }
        public string? CaseClasifyNo { get; set; }
        public string? ImpotrtantAnnexText { get; set; }
        public string? DocImage { get; set; }
        public string? DocImage_Base64 { get; set; }
        public string? ADVOCACYENDDATE { get; set; }
        public List<PersonInQuery>? LstFindPersonInQuery { get; set; }
    }
}
