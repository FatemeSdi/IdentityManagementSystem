using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace IdentityManagementSystem.API.Helpers
{
    public class EncryptionHelper
    {
        private readonly byte[] _key;
        private const string DefaultSalt = "Shamel2026_PM0_!@#";

        public EncryptionHelper(IConfiguration configuration)
        {
            var keyBase64 = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException("کلید رمزنگاری در appsettings.json تنظیم نشده است.");

            _key = Convert.FromBase64String(keyBase64);
        }

        // ---------------- ENCRYPT ----------------
        public string Encrypt(string? plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // IV + Cipher
            byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];

            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        // ---------------- DECRYPT (FIXED) ----------------
        public string Decrypt(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            // ✅ مهم: جلوگیری از crash روی داده‌های قدیمی (plaintext)
            if (!IsBase64(cipherText))
                return cipherText;

            byte[] combined;

            try
            {
                combined = Convert.FromBase64String(cipherText);
            }
            catch
            {
                // اگر Base64 خراب بود، همون متن رو برگردون
                return cipherText;
            }

            if (combined.Length < 17) // IV + data حداقل
                return cipherText;

            using var aes = Aes.Create();
            aes.Key = _key;

            byte[] iv = new byte[16];
            Array.Copy(combined, 0, iv, 0, 16);
            aes.IV = iv;

            byte[] cipher = new byte[combined.Length - 16];
            Array.Copy(combined, 16, cipher, 0, cipher.Length);

            try
            {
                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                // اگر decrypt شکست خورد، fallback به مقدار اصلی
                return cipherText;
            }
        }

        // ---------------- SAFE CHECK ----------------
        private bool IsBase64(string input)
        {
            Span<byte> buffer = new Span<byte>(new byte[input.Length]);
            return Convert.TryFromBase64String(input, buffer, out _);
        }

        // ---------------- HASH ----------------
        public string Hash(string? value, string? customSalt = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string salt = customSalt ?? DefaultSalt;

            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(salt + value);
            byte[] hashBytes = sha256.ComputeHash(bytes);

            return Convert.ToHexString(hashBytes).ToLower();
        }

        // ---------------- KEY GENERATOR ----------------
        public static string GenerateNewKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();

            return Convert.ToBase64String(aes.Key);
        }
    }
}