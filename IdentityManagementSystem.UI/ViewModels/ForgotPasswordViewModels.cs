using System.ComponentModel.DataAnnotations;

namespace IdentityManagementSystem.UI.ViewModels
{
    public class ForgotPasswordStartViewModel
    {
        [StringLength(10)]
        public string? NationalId { get; set; }

        [StringLength(11)]
        public string? MobileNumber { get; set; }
    }

    public class ForgotPasswordIdentityRequest
    {
        public string? NationalId { get; set; }
        public string? MobileNumber { get; set; }
    }

    public class ForgotPasswordVerifyRequest
    {
        public string? NationalId { get; set; }
        public string? MobileNumber { get; set; }
        public string? Code { get; set; }
    }

    public class ForgotPasswordResetRequest
    {
        public string? ResetToken { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
    }
}
