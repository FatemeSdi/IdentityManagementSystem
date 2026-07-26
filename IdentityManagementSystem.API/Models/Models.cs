using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagementSystem.API.Models
{
    [Table("Users", Schema = "Sec")]
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

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(75)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(85)]
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        [StringLength(11)]
        public string? MobileNumber { get; set; }

        // Navigation for many-to-many roles via UserRoles
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Convenience for single role (common in this app) - not mapped to DB
        [NotMapped]
        public Role? Role { get; set; }

        [NotMapped]
        public int RoleId { get; set; }
    }

    [Table("Roles", Schema = "Sec")]
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

    [Table("UserRoles", Schema = "Sec")]
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

    [Table("CartableItems", Schema = "WF")]
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

        public bool? IsExist { get; set; }
        public bool? IsRead { get; set; } = false;
        public string? ReadBy { get; set; }
        public DateTime? ReadDate { get; set; }
    }

    [Table("RefreshTokens", Schema = "Sec")]
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

    // ViewModels kept for compatibility (moved from controllers where possible)
    public class RoleViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}