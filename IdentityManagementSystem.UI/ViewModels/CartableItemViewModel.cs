using System.ComponentModel.DataAnnotations;

namespace IdentityManagementSystem.UI.ViewModels
{
    public class CartableItemViewModel
    {
        public long ItemId { get; set; }
        public long RequestId { get; set; }
        public string? NationalId { get; set; }
        public string? DocumentNumber { get; set; }
        public string? VerificationCode { get; set; }
        public string? MobileNumber { get; set; }
        public bool IdentityVerified { get; set; }
        public bool DocumentVerified { get; set; }
        public bool DocumentMatch { get; set; }
        public bool? ValidateByExpert { get; set; } // اضافه شده
        public string? Description { get; set; }
        public bool TextApproved { get; set; }
        public string? RequestStatus { get; set; }
        public long? AssignedTo { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ViewedAt { get; set; }
        public string? Status { get; set; }
        public bool? IsMatch { get; internal set; }
        public bool? IsExist { get; internal set; }
        public bool? IsNationalIdInResponse { get; set; }
        public bool? IsNationalIdInLawyers { get; set; }
        public DateTime CreatedAt { get; internal set; }
        public string? ImpotrtantAnnexText { get;  set; }
    }

    public class AssignCartableItemViewModel
    {
        [Required]
        public long ItemId { get; set; }
        [Required]
        public long AssignedTo { get; set; }
    }
    public class CartableFormViewModel
    {
        public string NationalCode { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string VerifyCode { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string ClientNationalCode { get; set; } = string.Empty;
        public bool AgreeToTerms { get; set; }
        public string Step1Message { get; set; } = string.Empty;
        public string Step2Message { get; set; } = string.Empty;
        public string Step3Message { get; set; } = string.Empty;
    }

    //public class CartableItemViewModel
    //{
    //    public long RequestId { get; set; }
    //    public string RequestCode { get; set; } = string.Empty;
    //    public string NationalId { get; set; } = string.Empty;
    //    public string MobileNumber { get; set; } = string.Empty;
    //    public string DocumentNumber { get; set; } = string.Empty;
    //    public string VerificationCode { get; set; } = string.Empty;
    //    public string ImpotrtantAnnexText { get; set; } = string.Empty;
    //    public bool? IsMatch { get; set; }
    //    public bool? IsExist { get; set; }
    //    public bool? IsNationalIdInResponse { get; set; }
    //    public bool? IsNationalIdInLawyers { get; set; }
    //    public bool? ValidateByExpert { get; set; }
    //    public string Description { get; set; } = string.Empty;
    //    public DateTime CreatedAt { get; set; }
    //    public string CreatedBy { get; set; } = string.Empty;
    //}

    public class PaginatedCartableViewModel
    {
        public List<CartableItemViewModel> Items { get; set; } = new List<CartableItemViewModel>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string SearchQuery { get; set; } = string.Empty;
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
