using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagementSystem.API.Models.ViewModels
{
    public class UserViewModel
    {

        public long UserId { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public required string Role { get; set; }               // ارتباط به جدول Roles
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }      
        public bool IsActive { get; set; }
        public string? MobileNumber { get; set; }      // اختیاری، می‌تونه null باشه
    }

    public class CreateUserViewModel
    {
        [Required]
        [StringLength(10)]
        public string? NationalId { get; set; }
        [Required]
        [StringLength(75)]
        public string? Name { get; set; }
        [Required]
        [StringLength(85)]
        public string? LastName { get; set; }
        [Required]
        [StringLength(255)]
        public string? Password { get; set; }

        public string? MobileNumber { get; set; }


        [Required]
        [StringLength(50)]
        public string? Username { get; set; }

        [Required]
        public int RoleId { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [StringLength(50)]
        public string? Username { get; set; }

        [Required]
        [StringLength(255)]
        public string? Password { get; set; }
    }
    public class GrantAccessViewModel
    {
        public long UserId { get; set; }
        [Required]
        [StringLength(50)]
        public string Permission { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? NationalId { get; set; }
        public string? MobileNumber { get; set; }
        public int? RoleId { get; set; }
    }
}
