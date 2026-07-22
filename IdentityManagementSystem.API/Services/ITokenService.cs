using IdentityManagementSystem.API.Models;
using System.Threading.Tasks;

namespace IdentityManagementSystem.API.Services
{
    public interface ITokenService
    {
        Task<object> GenerateTokensAsync(User user);
        Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task<object> RevokeRefreshTokenAsync(string refreshToken);
    }
}