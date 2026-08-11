using IdentityManagementSystem.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagementSystem.API.Models
{
    [Table("User", Schema = "Sec")]
    public class User
    {
        [Key]
        public long UserId { get; set; }

        [Required]
        [StringLength(10)]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        // دیگه Required نیست: کاربران Applicant پسورد ندارن (فقط OTP).
        // برای Staff همیشه باید پر باشه؛ این الزام توی CK_Users_PasswordRequiredForStaff
        // سطح DB و توی منطق ثبت‌نام Staff سمت سرویس چک می‌شه.
        [StringLength(255)]
        public string? PasswordHash { get; set; }

        [Required]
        [StringLength(75)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(85)]
        public string LastName { get; set; } = string.Empty;

        // 'Staff' (یوزرنیم+پسورد+OTP) یا 'Applicant' (فقط موبایل+OTP، بدون پسورد)
        //[Required]
        //[StringLength(20)]
        //public string UserType { get; set; } = "Staff";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        [StringLength(11)]
        public string? MobileNumber { get; set; }

        // شماره داخلی — برای تماس متقاضی با کارشناسی که درخواستش رو رد/تایید کرده
        [StringLength(10)]
        public string? Extension { get; set; }

        // Navigation for many-to-many roles via UserRoles
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Navigation for many-to-many groups via UserGroups
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

        // Convenience for single role (common in this app) - not mapped to DB
        [NotMapped]
        public Role? Role { get; set; }

        [NotMapped]
        public int RoleId { get; set; }
        public string? NationalIdEnc { get; set; }
        public string? NationalIdHash { get; set; }
    }

    [Table("Role", Schema = "Sec")]
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    [Table("UserRole", Schema = "Sec")]
    public class UserRole
    {
        [Key]
        public long UserRoleId { get; set; }

        public long UserId { get; set; }
        public User? User { get; set; }

        public int RoleId { get; set; }
        public Role? Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }

    [Table("Request", Schema = "Define")]
    public class Request
    {
        [Key]
        public long RequestId { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string? NationalId { get; set; }

        [StringLength(20)]
        public string? MobileNumber { get; set; }

        [StringLength(50)]
        public string? DocumentNumber { get; set; }

        [StringLength(50)]
        public string? VerificationCode { get; set; }

        [StringLength(50)]
        public string? WarehouseReceiptNumber { get; set; }

        // کد پیگیری قابل‌جستجو و مناسب پیامک — مثلاً REQ00020198 (مشتق‌شده از RequestId، همیشه یکتا)
        [StringLength(30)]
        public string? TrackingCode { get; set; }

        public bool? IsMatch { get; set; }
        public bool? IsExist { get; set; }
        public bool? IsNationalIdInResponse { get; set; }
        public bool? IsNationalIdInLawyers { get; set; }

        // گروه کارشناسی که این درخواست باید بره تو کارتابلش (بر اساس نوع درخواست تعیین می‌شه)
        public int? GroupId { get; set; }
        public Group? Group { get; set; }

        // کارشناسی که این درخواست رو Take کرده (NULL = هنوز کسی take نکرده)
        public long? AssignedTo { get; set; }
        public User? AssignedToUser { get; set; }
        public DateTime? AssignedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public bool? ValidateByExpert { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
    }

    [Table("Cartable", Schema = "WF")]
    public class Cartable
    {
        [Key]
        public long CartableId { get; set; }

        public long UserId { get; set; }
        public User? User { get; set; }

        [StringLength(200)]
        public string? CartableName { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("CartableItem", Schema = "WF")]
    public class CartableItem
    {
        [Key]
        public long ItemId { get; set; }

        public long CartableId { get; set; }
        public Cartable? Cartable { get; set; }

        public long RequestId { get; set; }
        public Request? Request { get; set; }

        public long? AssignedTo { get; set; }
        public User? AssignedToUser { get; set; }

        public DateTime? AssignedAt { get; set; }
        public DateTime? ViewedAt { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = "New";

        // These are used in code but not present in current DB schema.
        // Kept as NotMapped to avoid EF errors. Add columns to DB if needed.
        [NotMapped]
        public bool? ValidateByExpert { get; set; }

        [NotMapped]
        public string? Description { get; set; }
    }

    [Table("UserLog", Schema = "Log")]
    public class UserLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LogId { get; set; }

        public long UserId { get; set; }  // NOT NULL in DB

        [MaxLength(255)]
        public string? Action { get; set; }

        public DateTime? ActionTime { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(255)]
        public string? UserAgent { get; set; }

        [MaxLength(500)]
        public string? ActionResult { get; set; }

        [MaxLength(20)]
        public string? LogLevel { get; set; }
    }

    [Table("RequestHistory", Schema = "Sec")]
    public class RequestHistory
    {
        [Key]
        public long LogId { get; set; }

        public long RequestId { get; set; }
        public Request? Request { get; set; }

        public int StatusId { get; set; }
        public RequestStatus? Status { get; set; }

        // In DB: nvarchar(50) NOT NULL. Store as string (e.g. UserId.ToString() or Username)
        [Required]
        [StringLength(50)]
        public string ExpertId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ActionDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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

        [StringLength(200)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("UserAccess", Schema = "Sec")]
    public class UserAccess
    {
        [Key]
        [Column("AccessId")]
        public long Id { get; set; }

        public long UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(50)]
        public string Permission { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("Permission", Schema = "Sec")]
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }

        [Required]
        [StringLength(200)]
        public string PermissionName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PermissionCode { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("RolePermission", Schema = "Sec")]
    public class RolePermission
    {
        [Key]
        public long RolePermissionId { get; set; }

        public int RoleId { get; set; }
        public Role? Role { get; set; }

        public int PermissionId { get; set; }
        public Permission? Permission { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }

    // ---------------------------------------------------------------------
    // گروه‌ها: یک گروه مجموعه‌ای از Permission هاست و کاربران می‌تونن عضو یک یا چند گروه باشن
    // ---------------------------------------------------------------------
    [Table("Group", Schema = "Sec")]
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public ICollection<GroupPermission> GroupPermissions { get; set; } = new List<GroupPermission>();
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }

    [Table("GroupPermission", Schema = "Sec")]
    public class GroupPermission
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group? Group { get; set; }

        public int PermissionId { get; set; }
        public Permission? Permission { get; set; }
    }

    [Table("UserGroup", Schema = "Sec")]
    public class UserGroup
    {
        [Key]
        public int Id { get; set; }

        public long UserId { get; set; }
        public User? User { get; set; }

        public int GroupId { get; set; }
        public Group? Group { get; set; }
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

        public bool IsMatch { get; set; }

        public string? ResponseText { get; set; }

        public long ExpertId { get; set; }

        public long? RequestId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? NationalIdEnc { get; set; }
        public string? NationalIdHash { get; set; }
        public string? MobileNumberEnc { get; set; }
        public string? MobileNumberHash { get; set; }
    }

    [Table("VerifyDocLog", Schema = "Log")]
    public class VerifyDocLog
    {
        [Key]
        public int VerifyDocLogId { get; set; }

        [Required]
        public string ResponseText { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string DocumentNumber { get; set; } = null!;

        public long? RequestId { get; set; }

        [Required]
        [StringLength(10)]
        public string VerificationCode { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = null!;

        public string? DocumentNumberEnc { get; set; }
        public string? DocumentNumberHash { get; set; }
        public string? VerificationCodeEnc { get; set; }
        public string? VerificationCodeHash { get; set; }

        public bool? IsExist { get; set; }
        public bool? IsRead { get; set; } = false;
        public string? ReadBy { get; set; }
        public DateTime? ReadDate { get; set; }
    }

    [Table("Sms", Schema = "Log")]
    public class SmsLog
    {
        [Key]
        public long SmsId { get; set; }

        public long? UserId { get; set; }
        public long? RequestId { get; set; }
        public long? OtpId { get; set; }

        [Required]
        [StringLength(256)]
        public string MobileNumberEnc { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string MobileNumberHash { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Purpose { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MessageTemplate { get; set; }

        [Required]
        [StringLength(500)]
        public string MessageText { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ProviderMessageId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
    }

    [Table("WarehouseReceipt", Schema = "Define")]
    public class WarehouseReceipt
    {
        [Key]
        public long WarehouseReceiptId { get; set; }

        public long RequestId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SerialNumber { get; set; }

        [StringLength(20)]
        public string? OwnerNationalId { get; set; }

        [StringLength(500)]
        public string? GoodsDescription { get; set; }

        public decimal? Quantity { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; }

        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? StatusId { get; set; }
        public bool? IsVerified { get; set; }

        [StringLength(50)]
        public string? VerificationCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }

    [Table("WarehouseReceiptLog", Schema = "Log")]
    public class WarehouseReceiptLog
    {
        [Key]
        public long WarehouseReceiptLogId { get; set; }

        public long? RequestId { get; set; }

        [Required]
        [StringLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        // مشخص می‌کنه این لاگ مال کدوم سرویس bsr-* بوده (PortIncomeInvoice, ServiceCostInvoice, InsuranceInvoice, ParkingCostInvoice)
        [StringLength(50)]
        public string? ServiceType { get; set; }

        public string? ResponseText { get; set; }

        public bool IsSuccessful { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }

    [Table("RefreshToken", Schema = "Sec")]
    public class RefreshToken
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }

    // ---------------------------------------------------------------------
    // OTP (لاگین Staff با پسورد+OTP، ثبت‌نام/لاگین Applicant فقط با OTP)
    // ---------------------------------------------------------------------
    [Table("LoginOtp", Schema = "Sec")]
    public class LoginOtp
    {
        [Key]
        public long OtpId { get; set; }

        public long UserId { get; set; }
        public User? User { get; set; }

        // فقط هش کد نگه‌داری می‌شه، هرگز خود کد (با ComputeSearchHash از EncryptionHelper)
        [Required]
        [StringLength(64)]
        public string OtpCodeHash { get; set; } = string.Empty;

        // 'Registration' | 'Login' | 'StaffLogin2FA'
        [Required]
        [StringLength(20)]
        public string Purpose { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public int AttemptCount { get; set; } = 0;

        public int MaxAttempts { get; set; } = 5;

        public bool IsVerified { get; set; } = false;

        public DateTime? VerifiedAt { get; set; }

        public bool IsUsed { get; set; } = false;

        [StringLength(45)]
        public string? IpAddress { get; set; }
    }

    // ---------------------------------------------------------------------
    // لاگ پیامک‌های ارسالی (OTP و اطلاع‌رسانی مراحل درخواست)
    // ---------------------------------------------------------------------
    [Table("Sms", Schema = "Log")]
    public class Sms
    {
        [Key]
        public long SmsId { get; set; }

        public long? UserId { get; set; }
        public User? User { get; set; }

        public long? RequestId { get; set; }
        public Request? Request { get; set; }

        public long? OtpId { get; set; }
        public LoginOtp? LoginOtp { get; set; }

        [Required]
        [StringLength(256)]
        public string MobileNumberEnc { get; set; } = string.Empty;

        [StringLength(64)]
        public string? MobileNumberHash { get; set; }

        // 'Otp' | 'RequestStageUpdate' | 'RequestApproved' | 'RequestRejected' | 'General'
        [Required]
        [StringLength(30)]
        public string Purpose { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MessageTemplate { get; set; }

        // متن باید همیشه mask‌شده ذخیره بشه (کد OTP هرگز plaintext وارد دیتابیس نشه)
        [Required]
        [StringLength(500)]
        public string MessageText { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ProviderMessageId { get; set; }

        // 'Queued' | 'Sent' | 'Failed' | 'DeliveryUnknown'
        [StringLength(20)]
        public string Status { get; set; } = "Queued";

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }
    }

    // ViewModels kept for compatibility (moved from controllers where possible)
    public class RoleViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}