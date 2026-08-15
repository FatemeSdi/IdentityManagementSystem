using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace IdentityManagementSystem.API.Helpers
{
    public class EncryptionHelper
    {
        private readonly byte[] _key;
        private readonly byte[] _hmacKey;
        private const string DefaultSalt = "Shamel2026_PM0_!@#";

        public EncryptionHelper(IConfiguration configuration)
        {
            var keyBase64 = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException("کلید رمزنگاری در appsettings.json تنظیم نشده است.");
            _key = Convert.FromBase64String(keyBase64);

            var hmacKeyBase64 = configuration["Encryption:HmacKey"]
                ?? throw new InvalidOperationException("کلید HMAC (برای جستجوی داده‌های رمزنگاری‌شده) در appsettings.json تنظیم نشده است.");
            _hmacKey = Convert.FromBase64String(hmacKeyBase64);
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

        // ---------------- DECRYPT ----------------
        public string Decrypt(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            if (!IsBase64(cipherText))
                return cipherText;

            byte[] combined;

            try
            {
                combined = Convert.FromBase64String(cipherText);
            }
            catch
            {
                return cipherText;
            }

            if (combined.Length < 17)
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
                return cipherText;
            }
        }

        // ---------------- DECRYPT WITH FALLBACK ----------------
        // برای فیلدهایی که تازه به رمزنگاری منتقل شدن: رکوردهای جدید فقط ستون Enc رو پر می‌کنن،
        // رکوردهای قدیمی‌تر هنوز فقط ستون plaintext قدیمی رو دارن. این متد یکجا هر دو حالت رو
        // پوشش می‌ده تا نمایش (مثلاً کارتابل کارشناس) هیچ‌وقت نشکنه.
        public string? DecryptOrFallback(string? encryptedValue, string? plaintextFallback)
        {
            return !string.IsNullOrEmpty(encryptedValue) ? Decrypt(encryptedValue) : plaintextFallback;
        }

        // ---------------- SEARCH HASH (Blind Index) ----------------
        // برای جستجو و uniqueness check روی داده‌های encrypted استفاده می‌شود.
        // برخلاف Encrypt، این تابع deterministic است (بدون IV تصادفی)
        // پس فقط برای مقادیر شناسه‌ای/جستجوپذیر استفاده شود، نه برای داده‌ای که محرمانگی معنایی بالا نیاز دارد.
        public string ComputeSearchHash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = Normalize(value);

            using var hmac = new HMACSHA256(_hmacKey);
            byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(bytes).ToLower();
        }

        // نرمال‌سازی قبل از هش کردن: بدون این کار، تفاوت‌های جزئی (space، نویسه عربی/فارسی)
        // باعث می‌شود همان مقدار دو hash متفاوت تولید کند و تشخیص تکراری از کار بیفتد.
        private string Normalize(string value)
        {
            return value
                .Trim()
                .Replace("ي", "ی")
                .Replace("ك", "ک")
                .Replace(" ", ""); // برای کد ملی/شماره سند که نباید فاصله معنادار داشته باشند
        }

        // ---------------- SIGN / VERIFY PAYLOAD (stateless tokens, e.g. password-reset) ----------------
        // برای صدور توکن‌های کوتاه‌عمر بدون نیاز به ذخیره‌سازی جداگانه در DB (مثلاً resetToken بازیابی رمز عبور)
        public string SignPayload(string payload)
        {
            using var hmac = new HMACSHA256(_hmacKey);
            byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes).ToLower();
        }

        public bool VerifyPayload(string payload, string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return false;

            var expected = SignPayload(payload);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            var actualBytes = Encoding.UTF8.GetBytes(signature.ToLower());

            if (expectedBytes.Length != actualBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        // ---------------- SAFE CHECK ----------------
        private bool IsBase64(string input)
        {
            Span<byte> buffer = new Span<byte>(new byte[input.Length]);
            return Convert.TryFromBase64String(input, buffer, out _);
        }

        // ---------------- HASH (legacy - غیرمرتبط با blind index) ----------------
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

        // ---------------- KEY GENERATORS ----------------
        public static string GenerateNewKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();

            return Convert.ToBase64String(aes.Key);
        }

        public static string GenerateNewHmacKey()
        {
            using var hmac = new HMACSHA256();
            return Convert.ToBase64String(hmac.Key);
        }
    }
}