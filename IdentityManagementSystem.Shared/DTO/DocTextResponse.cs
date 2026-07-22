namespace IdentityManagementSystem.API.DTO
{
    public class DocTextResponse
    {
        public bool Success { get; set; }
        public string? DocumentText { get; set; }
        public bool IsRead { get; set; }
        public bool ExistDoc { get; set; } // اضافه شده برای هماهنگی با VerifyDocResponse
        public string? Message { get; set; }
    }
}
