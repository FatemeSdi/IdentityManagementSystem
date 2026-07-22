using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityManagementSystem.API.Services
{
    public class TokenService : ITokenService
    {
        private readonly IdentityManagementSystemContext _context;
        private readonly IConfiguration _configuration;

        public TokenService(IdentityManagementSystemContext context, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// تولید Access Token و Refresh Token برای کاربر.
        /// Access Token با اعتبار ۱۵ دقیقه و Refresh Token با اعتبار ۷ روز تولید شده و در دیتابیس ذخیره می‌شود.
        /// </summary>
        /// <param name="user">کاربری که برای آن توکن تولید می‌شود</param>
        /// <returns>شیء شامل Access Token و Refresh Token</returns>
        public async Task<object> GenerateTokensAsync(User user)
        {
            var accessToken = await GenerateJwtToken(user);
            var refreshTokenString = Guid.NewGuid().ToString();
            var refreshToken = new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshTokenString,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };
        }

        /// <summary>
        /// بازیابی یک Refresh Token خاص از دیتابیس.
        /// بررسی می‌کند که توکن معتبر (غیرباطل و غیرمنقضی) باشد.
        /// </summary>
        /// <param name="refreshToken">مقدار Refresh Token</param>
        /// <returns>شیء RefreshToken یا null اگر توکن نامعتبر باشد</returns>
        public async Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken
                    && rt.ExpiryDate > DateTime.UtcNow
                    && !rt.IsRevoked);
        }

        /// <summary>
        /// باطل کردن یک Refresh Token خاص.
        /// توکن را در دیتابیس پیدا کرده و پرچم IsRevoked را به true تنظیم می‌کند.
        /// </summary>
        /// <param name="refreshToken">مقدار Refresh Token</param>
        /// <returns>شیء با وضعیت موفقیت و پیام مربوطه</returns>
        public async Task<object> RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (token == null)
            {
                return new { Success = false, Message = "Refresh Token یافت نشد." };
            }

            token.IsRevoked = true;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();

            return new { Success = true, Message = "Refresh Token با موفقیت باطل شد." };
        }

        /// <summary>
        /// باطل کردن تمام Refresh Token‌های فعال یک کاربر.
        /// تمام توکن‌های غیرباطل و غیرمنقضی کاربر را پیدا کرده و باطل می‌کند.
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <returns>شیء با وضعیت موفقیت و پیام مربوطه</returns>
        public async Task<object> RevokeAllRefreshTokensAsync(long userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiryDate > DateTime.UtcNow)
                .ToListAsync();

            if (!tokens.Any())
            {
                return new { Success = false, Message = "هیچ Refresh Token فعالی برای این کاربر یافت نشد." };
            }

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _context.RefreshTokens.Update(token);
            }

            await _context.SaveChangesAsync();

            return new { Success = true, Message = "تمام Refresh Token‌های فعال کاربر با موفقیت باطل شدند." };
        }

        /// <summary>
        /// تولید Access Token (JWT) برای کاربر.
        /// اطلاعات کاربر و دسترسی‌های او را به توکن اضافه کرده و با کلید JWT امضا می‌کند.
        /// </summary>
        /// <param name="user">کاربری که برای آن توکن تولید می‌شود</param>
        /// <returns>رشته JWT با اعتبار ۱۵ دقیقه</returns>
        private async Task<string> GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var permissions = await _context.UserAccesses
                .Where(ua => ua.UserId == user.UserId)
                .Select(ua => ua.Permission)
                .ToListAsync();

            var claims = new List<Claim>
{
    new Claim("UserId", user.UserId.ToString()),
    new Claim("Username", user.Username),

    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

    // این مهمه
    new Claim(ClaimTypes.Name, user.Username),

    new Claim("FullName", user.Name ?? ""),

    new Claim(ClaimTypes.Surname, user.LastName ?? ""),

    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
};
            // اضافه کردن پرمیشن‌ها
            claims.AddRange(permissions.Select(p => new Claim("Permission", p)));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}