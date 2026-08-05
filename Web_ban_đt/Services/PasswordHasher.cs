using System.Security.Cryptography;

namespace TechStoreWeb.Services
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        PasswordVerificationResult Verify(string? storedPassword, string providedPassword);
    }

    public enum PasswordVerificationResult
    {
        Failed = 0,
        Success = 1,
        SuccessNeedsUpgrade = 2
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const string Prefix = "PBKDF2";
        private const int Iterations = 210_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public PasswordVerificationResult Verify(string? storedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword) || string.IsNullOrEmpty(providedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            if (!storedPassword.StartsWith(Prefix + "$", StringComparison.Ordinal))
            {
                return CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.UTF8.GetBytes(storedPassword),
                        System.Text.Encoding.UTF8.GetBytes(providedPassword))
                    ? PasswordVerificationResult.SuccessNeedsUpgrade
                    : PasswordVerificationResult.Failed;
            }

            var parts = storedPassword.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
            {
                return PasswordVerificationResult.Failed;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(expected, actual)
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
        }
    }
}
