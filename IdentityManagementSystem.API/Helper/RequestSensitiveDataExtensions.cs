using IdentityManagementSystem.API.Models;

namespace IdentityManagementSystem.API.Helpers
{
    /// <summary>
    /// خواندن داده‌ی حساس متقاضی (موبایل/کد ملی) از جدول جزئیاتِ نوع درخواست — قبض انبار یا ارزیابی.
    /// Request باید با WareReciept/Eval بارگذاری شده باشه (Include)، وگرنه null برمی‌گرده.
    /// </summary>
    public static class RequestSensitiveDataExtensions
    {
        public static string? DecryptMobile(this EncryptionHelper helper, Request request)
        {
            if (request.WareReciept != null)
                return helper.DecryptOrFallback(request.WareReciept.MobileNumberEnc, request.WareReciept.MobileNumber);
            if (request.Eval != null)
                return helper.DecryptOrFallback(request.Eval.MobileNumberEnc, null);
            return null;
        }

        public static string? DecryptNationalId(this EncryptionHelper helper, Request request)
        {
            if (request.WareReciept != null)
                return helper.DecryptOrFallback(request.WareReciept.NationalIdEnc, request.WareReciept.NationalId);
            if (request.Eval != null)
                return helper.DecryptOrFallback(request.Eval.NationalIdEnc, null);
            return null;
        }
    }
}
