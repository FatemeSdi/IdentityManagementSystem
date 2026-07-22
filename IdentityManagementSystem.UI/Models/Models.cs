using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagementSystem.UI.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }

        [Required]
        [StringLength(10)]
        public string? NationalId { get; set; }

        [Required]
        [StringLength(50)]
        public string? Username { get; set; }

        [Required]
        [StringLength(255)]
        public string? PasswordHash { get; set; }

        [Required]
        [StringLength(75)]
        public string? Name { get; set; }

        [Required]
        [StringLength(85)]
        public string? LastName { get; set; }

        [Required]
        [StringLength(50)]
        public string? Role { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class Request
    {
        [Key]
        public long RequestId { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestCode { get; set; } = string.Empty;

        [StringLength(10)]
        public string? NationalId { get; set; }     // کد ملی

        [StringLength(20)]
        public string? MobileNumber { get; set; }   // شماره همراه

        [StringLength(50)]
        public string? DocumentNumber { get; set; } // شناسه سند

        [StringLength(50)]
        public string? VerificationCode { get; set; } // رمز تصدیق

        public bool? IsMatch { get; set; }              // وضعیت احراز هویت
        public bool? IsExist { get; set; }              // وجود سند
        public bool? IsNationalIdInResponse { get; set; } // بررسی نهایی
        public bool? IsNationalIdInLawyers { get; set; } // بررسی نهایی

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }

    public class Cartable
    {
        [Key]
        public long CartableId { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }

        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CartableItem
    {
        [Key]
        public long ItemId { get; set; }

        [ForeignKey("Cartable")]
        public long CartableId { get; set; }

        public Cartable? Cartable { get; set; }

        [ForeignKey("Request")]
        public long RequestId { get; set; }

        public Request? Request { get; set; }

        [ForeignKey("User")]
        public long? AssignedTo { get; set; }

        public User? AssignedToUser { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ViewedAt { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "New";
    }

    public class UserLog
    {
        public long LogId { get; set; }
        public long UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ActionResult { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string LogLevel { get; set; } = "Info"; // پیش‌فرض
    }


    [Table("ShahkarLog", Schema = "Log")]
    public class ShahkarLog
    {
        [Key]
        public long LogId { get; set; }

        [Required]
        [StringLength(10)]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [StringLength(11)]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string RequestCode { get; set; } = string.Empty;

        [Required]
        public bool IsMatch { get; set; }

        public string? ResponseText { get; set; }

        [Required]
        public long ExpertId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RequestLog
    {
        [Key]
        public long LogId { get; set; }

        [ForeignKey("Request")]
        public long RequestId { get; set; }

        public Request? Request { get; set; }

        [ForeignKey("User")]
        public long? UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [StringLength(50)]
        public string? Action { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.UtcNow;

        public string? Details { get; set; }
    }

    [Table("VerifyDocLog", Schema = "Log")]
    public class VerifyDocLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [ForeignKey(nameof(ShahkarLog))]
        public long ShahkarLogId { get; set; }

        [Required]
        [StringLength(10)]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [StringLength(11)]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string VerificationCode { get; set; } = string.Empty;

        [Required]
        public bool IsExist { get; set; }

        public string? ResponseText { get; set; }

        [Required]
        public long ExpertId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ShahkarLog ShahkarLog { get; set; } = null!;
    }
}