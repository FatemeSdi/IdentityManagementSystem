using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace IdentityManagementSystem.API.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }

        // فیلدهای رمزنگاری شده جدید
        
        public string NationalId { get; set; } = string.Empty;   // برای جستجو

        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public string? MobileNumber { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        
    }

    public class RoleViewModel
        {
            public int RoleId { get; set; }
            public string RoleName { get; set; } = string.Empty;
        }


    public class Request
    {
        [Key]
        public long RequestId { get; set; }

        [StringLength(10)]
        public string? NationalIdEnc { get; set; }

        public string? NationalIdHash { get; set; }        // برای جستجوی سریع

        [StringLength(20)]
        public string? MobileNumberEnc { get; set; }

        public string? MobileNumberHash { get; set; }

        [StringLength(50)]
        public string? DocumentNumberEnc { get; set; }

        [StringLength(50)]
        public string? VerificationCodeEnc { get; set; }

        public string? RequestCode { get; set; } = string.Empty;   // این را هم می‌توانی encrypt کنی اگر حساس باشد

        public bool? ValidateByExpert { get; set; }
        public string? Description { get; set; }

        public bool? IsMatch { get; set; }
        public bool? IsExist { get; set; }
        public bool? IsNationalIdInResponse { get; set; }
        public bool? IsNationalIdInLawyers { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // فیلدهای قدیمی (برای مهاجرت)
        [Obsolete("Use NationalIdEnc instead")]
        public string? NationalId { get; set; }

        [Obsolete("Use MobileNumberEnc instead")]
        public string? MobileNumber { get; set; }

        [Obsolete("Use DocumentNumberEnc instead")]
        public string? DocumentNumber { get; set; }

        [Obsolete("Use VerificationCodeEnc instead")]
        public string? VerificationCode { get; set; }
    }

    public class Cartable
    {
        [Key]
        public long CartableId { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }

        public User? User { get; set; }
        public string? CartableName { get; set; }
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
        public bool? ValidateByExpert { get; set; } // اضافه شده
        public string? Description { get; set; }
        public User? AssignedToUser { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ViewedAt { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "New";
    }

    [Table("UserLog", Schema = "Log")]
    public class UserLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LogId { get; set; }

        public long? UserId { get; set; }

        [MaxLength(255)]
        public string? Action { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }
        public string ActionResult { get; internal set; }
        public string LogLevel { get; internal set; }
    }


    [Table("RequestHistory", Schema = "sec")]
    public class RequestHistory
    {
        [Key]
        public long LogId { get; set; }

        // ارتباط با جدول Request
        [ForeignKey(nameof(Request))]
        public long RequestId { get; set; }
        public Request? Request { get; set; }

        // وضعیت درخواست
   
         public int StatusId { get; set; }
        public RequestStatus Status { get; set; } = null!;

        // کارشناس انجام‌دهنده یا دریافت‌کننده
        [ForeignKey(nameof(Expert))]
        public long? ExpertId { get; set; }
        public User? Expert { get; set; }

        // توضیح عملکرد
        [StringLength(250)]
        public string? ActionDescription { get; set; }

        // زمان ایجاد لاگ
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🟢 فیلدهای مربوط به وضعیت به‌روزرسانی
        [StringLength(100)]
        public string? UpdatedStatus { get; set; }

        [StringLength(100)]
        public string? UpdatedStatusBy { get; set; }

        public DateTime? UpdatedStatusDate { get; set; }
    }

    [Table("RequestStatus", Schema = "Define")]
    public class RequestStatus
    {
        [Key]
        public int StatusId { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    // مدل جدید UserAccess
    public class UserAccess
    {
        [Key]
        [Column("AccessId")]
        public long Id { get; set; }

        [ForeignKey("User")]
        [Column("UserId")]
        public long UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Permission")]
        public string? Permission { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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

      
        public long RequestId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("VerifyDocLog", Schema = "Log")]
    public class VerifyDocLog
    {
        [Key]
        public int VerifyDocLogId { get; set; }

        // لاگ اصلی: کل JSON پاسخ سرویس
        [Required]
        public string ResponseText { get; set; } = null!;

        // مقادیر ورودی که بعدا برای تطابق کارتابل استفاده میشن
        [Required]
        public string DocumentNumber { get; set; } = null!;  // NationalRegisterNo

        public long RequestId { get; set; }
        [Required]
        public string VerificationCode { get; set; } = null!; // SecretNo

        // تاریخ و کاربر ایجاد کننده
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public string CreatedBy { get; set; } = null!;
        public bool? IsExist { get;  set; }
        public bool? IsRead { get; set; } = false; 
        public string? ReadBy { get; set; } 
        public DateTime? ReadDate { get; set; }
    }

    [Table("RefreshTokens", Schema = "Sec")]
    public class RefreshToken
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsRevoked { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

}