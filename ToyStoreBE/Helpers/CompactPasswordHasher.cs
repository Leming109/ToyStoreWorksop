using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace ToyStoreBE.Helpers
{
    public static class CompactPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 16;
        private const int Iterations = 600000;

        private const string Prefix = "P2$";

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                hash = pbkdf2.GetBytes(KeySize);
            }

            byte[] result = new byte[SaltSize + KeySize];

            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, result, SaltSize, KeySize);

            // Prefix 3 ký tự + Base64 44 ký tự = 47 ký tự
            return Prefix + Convert.ToBase64String(result);
        }

        public static bool VerifyPassword(
            string password,
            string storedPassword)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrWhiteSpace(storedPassword))
            {
                return false;
            }

            // NCHAR(50) tự bổ sung khoảng trắng cuối chuỗi
            storedPassword = storedPassword.TrimEnd();

            if (!storedPassword.StartsWith(
                Prefix,
                StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                byte[] data = Convert.FromBase64String(
                    storedPassword.Substring(Prefix.Length));

                if (data.Length != SaltSize + KeySize)
                    return false;

                byte[] salt = new byte[SaltSize];
                byte[] expectedHash = new byte[KeySize];

                Buffer.BlockCopy(data, 0, salt, 0, SaltSize);

                Buffer.BlockCopy(
                    data,
                    SaltSize,
                    expectedHash,
                    0,
                    KeySize);

                byte[] actualHash;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256))
                {
                    actualHash = pbkdf2.GetBytes(KeySize);
                }

                // So sánh theo thời gian cố định
                int difference = 0;

                for (int i = 0; i < KeySize; i++)
                {
                    difference |= expectedHash[i] ^ actualHash[i];
                }

                return difference == 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}