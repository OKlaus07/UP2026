using System;
using System.Security.Cryptography;

namespace ClubRegistration.Mvc.Services
{
    public static class PasswordService
    {
        private const int Iterations = 210000;

        public static byte[] NewSalt()
        {
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        // Новые пароли хешируются через PBKDF2-SHA256.
        public static string Hash(string password, byte[] salt)
        {
            byte[] hash = Derive(password, salt, Iterations, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(hash);
        }

        // Поддерживаем старые PBKDF2-SHA1 хеши, чтобы сохранённые аккаунты не потеряли доступ.
        public static bool Verify(string password, byte[] salt, string savedHash)
        {
            try
            {
                byte[] expected = Convert.FromBase64String(savedHash);
                byte[] modern = Derive(password ?? "", salt, Iterations, HashAlgorithmName.SHA256);
                if (CryptographicOperations.FixedTimeEquals(modern, expected)) return true;

                byte[] legacy = Derive(password ?? "", salt, 100000, HashAlgorithmName.SHA1);
                return CryptographicOperations.FixedTimeEquals(legacy, expected);
            }
            catch (FormatException) { return false; }
        }

        public static bool IsLegacyHash(string password, byte[] salt, string savedHash)
        {
            try
            {
                byte[] expected = Convert.FromBase64String(savedHash);
                byte[] legacy = Derive(password ?? "", salt, 100000, HashAlgorithmName.SHA1);
                return CryptographicOperations.FixedTimeEquals(legacy, expected);
            }
            catch (FormatException) { return false; }
        }

        // API совместим с .NET 5; для новых паролей используется PBKDF2-SHA256.
        private static byte[] Derive(string password, byte[] salt, int iterations, HashAlgorithmName algorithm)
        {
            using (var derivation = new Rfc2898DeriveBytes(password, salt, iterations, algorithm))
            {
                return derivation.GetBytes(32);
            }
        }
    }
}
