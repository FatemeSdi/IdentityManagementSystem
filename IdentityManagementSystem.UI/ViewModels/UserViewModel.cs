using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagementSystem.UI.ViewModels
{
    public class UserViewModel
    {

        public long UserId { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PasswordSalt { get; set; }       // اختیاری، می‌تونه null باشه
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public required string Role { get; set; }                 // ارتباط به جدول Roles
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }       // می‌تونه null باشه
        public bool IsActive { get; set; }
        public string? MobileNumber { get; set; }      // اختیاری، می‌تونه null باشه
        public int RoleId { get; internal set; }
    }

    // در فایل ViewModels
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "نام الزامی است")]
        public string Name { get; set; }

        [Required(ErrorMessage = "نام خانوادگی الزامی است")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "نام کاربری الزامی است")]
        public string Username { get; set; }

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [MinLength(6, ErrorMessage = "رمز عبور باید حداقل 6 کاراکتر باشد")]
        public string Password { get; set; }

        [Required(ErrorMessage = "تأیید رمز عبور الزامی است")]
        [Compare("Password", ErrorMessage = "رمز عبور و تأیید رمز عبور یکسان نیستند")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "کد ملی الزامی است")]
        public string NationalId { get; set; }

        [Required(ErrorMessage = "نقش کاربر الزامی است")]
        public int RoleId { get; set; }

        public string? MobileNumber { get; set; }
    }

    public class RoleViewModel
    {
        public int roleId { get; set; }
        public string roleName { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "فیلد {0} الزامی است")]
        [Display(Name = "نام کاربری")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "فیلد {0} الزامی است")]
        [Display(Name = "رمز عبور")]
        public string? Password { get; set; }
    }

    public class GrantAccessViewModel
    {
        public long UserId { get; set; }
        [Required]
        [StringLength(50)]
        public string Permission { get; set; }
    }
}
