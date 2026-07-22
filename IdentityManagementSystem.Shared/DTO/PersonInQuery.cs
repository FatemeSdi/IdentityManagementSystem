using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityManagementSystem.Shared.DTO
{
    public class PersonInQuery
    {
        public string? NationalNo { get; set; }
        public string? Birthdate { get; set; }
        public string? Name { get; set; }
        public string? Family { get; set; }
        public string? AgentType { get; set; }
        public string? PersonType { get; set; }
        public string? PersonType_code { get; set; }
        public string? NationalNoMovakel { get; set; }
        public string? NameMovakel { get; set; }
        public string? FamilyMovakel { get; set; }
        public string? TxtRelation { get; set; }
        public string? RoleType { get; set; }
        public string? Person_RoleType_code { get; set; }
    }
}
