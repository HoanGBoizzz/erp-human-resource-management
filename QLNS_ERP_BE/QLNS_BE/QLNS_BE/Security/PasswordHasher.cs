using System.Security.Cryptography;
using System.Text;

namespace QLNS_BE.Security
{
    public class PasswordHasher
    {
        /// <summary>
        /// Sinh password HASH + SALT dùng PBKDF2 (HMACSHA256)
        /// </summary>
        public void CreatePasswordHash(string password, out string hashBase64, out string saltBase64)
        {
            // Tạo SALT 32 bytes
            var salt = RandomNumberGenerator.GetBytes(32);

            // Hash theo chuẩn PBKDF2
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                100000,                   // độ mạnh
                HashAlgorithmName.SHA256,
                32                         // độ dài hash
            );

            hashBase64 = Convert.ToBase64String(hash);
            saltBase64 = Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Kiểm tra password có khớp hash + salt hay không
        /// </summary>
        public bool Verify(string password, string storedHashBase64, string storedSaltBase64)
        {
            var salt = Convert.FromBase64String(storedSaltBase64);
            var storedHash = Convert.FromBase64String(storedHashBase64);

            var newHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );

            return CryptographicOperations.FixedTimeEquals(newHash, storedHash);
        }
    }
}
