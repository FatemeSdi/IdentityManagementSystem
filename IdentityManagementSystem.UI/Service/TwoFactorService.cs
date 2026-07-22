namespace IdentityManagementSystem.Service
{
    // Services/TwoFactorService.cs
    public static class TwoFactorService
    {
        // صرفاً برای تست – بعداً میشه به SMS یا Google Authenticator وصل کرد
        public static bool ValidateCode(string username, string code)
        {
            // هر کی کد 123456 رو بزنه موفق میشه
            if (code == "123456")
                return true;

            return false;
        }
    }

}
