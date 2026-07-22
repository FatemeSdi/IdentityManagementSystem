using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagementSystem.API.Models.ViewModels
{

    public class RequestViewModel
    {
        public long RequestId { get; set; }
        [Required]
        [StringLength(10)]
        public string? NationalId { get; set; }
        [Required]
        [StringLength(11)]
        [Column("mobile_number")]
        public string? MobileNumber { get; set; }
        [Required]
        [StringLength(50)]
        public string? DocumentNumber { get; set; }
        [Required]
        [StringLength(50)]
        public string? VerificationCode { get; set; }
        public bool IdentityVerified { get; set; }
        public bool DocumentVerified { get; set; }
        public bool DocumentMatch { get; set; }
        public bool TextApproved { get; set; }
        public bool IsMatch { get; set; }
        public bool? ValidateByExpert { get; set; } 
        public string? Description { get; set; }
        public long? ExpertId { get; set; }
        public string? ExpertName { get; set; }
        public string? RequestStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? DocumentText { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public bool? IsExist { get;  set; }
        public bool? IsNationalIdInResponse { get; set; }
        public bool? IsNationalIdInLawyers { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class CreateRequestViewModel
    {
        [Required]
        [StringLength(10)]
        public string? NationalId { get; set; }
        [Required]
        [StringLength(50)]
        public string? DocumentNumber { get; set; }
        [Required]
        [StringLength(50)]
        public string? VerificationCode { get; set; }
        public string? DocumentText { get; set; }
        public string MobileNumber { get; internal set; }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class PaginationParameters
    {
        private const int MaxPageSize = 100;
        public int Page { get; set; } = 1;
        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Min(MaxPageSize, value); // محدود کردن به 100
        }
        public string? Search { get; set; }
    }
}
