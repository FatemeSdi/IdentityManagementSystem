using System.Threading.Tasks;

namespace IdentityManagementSystem.API.Services.Sms
{
    public interface ISmsService
    {
        Task<SmsResult> SendAsync(string to, string message);
    }

    public class SmsResult
    {
        public bool IsSuccess { get; set; }
        public string? RawResponse { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
