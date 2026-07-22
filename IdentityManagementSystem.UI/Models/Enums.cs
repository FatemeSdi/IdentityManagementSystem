// Enums/RejectReason.cs
using System.ComponentModel.DataAnnotations;

namespace IdentityManagementSystem.UI.Enums
{
    public enum RejectReason
    {
        [Display(Name = "عدم تطابق کد ملی و شماره همراه")]
        MismatchNationalIdMobile = 1,

        [Display(Name = "عدم وجود سند با این شناسه و رمز تصدیق")]
        DocumentNotFound = 2,

        [Display(Name = "عدم ارتباط متقاضی با سند (به عنوان وکیل)")]
        NoRelationToDocument = 3,

        [Display(Name = "عدم اعتبار وکالتنامه (منقضی شده)")]
        InvalidPowerOfAttorney = 4,

        [Display(Name = "سایر دلایل")]
        Other = 99
    }
}