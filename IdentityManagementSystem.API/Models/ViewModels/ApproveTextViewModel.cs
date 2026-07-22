using System.ComponentModel.DataAnnotations;

namespace IdentityManagementSystem.API.Models.ViewModels
{
    public class ApproveTextViewModel
    {
        [Required]
        public bool IsApproved { get; set; }
    }
}
