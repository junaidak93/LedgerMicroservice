using System.Security.Cryptography;
using System.Text;

namespace Ledger.API.Helpers;

public static class PasswordHasher
{
    private const int SaltSize = 16; // bytes
    private const int KeySize = 32; // bytes
    private const int Iterations = 100_000;

    // Format: pbkdf2_sha256$<iterations>$<base64salt>$<base64key>
    public static string HashPassword(string password)
    {
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(KeySize);

        return $"pbkdf2_sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        if (storedHash.StartsWith("pbkdf2_sha256$", StringComparison.Ordinal))
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4) return false;

            if (!int.TryParse(parts[1], out var iterations)) return false;

            var salt = Convert.FromBase64String(parts[2]);
            var key = Convert.FromBase64String(parts[3]);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var attempted = deriveBytes.GetBytes(key.Length);

            return CryptographicOperations.FixedTimeEquals(attempted, key);
        }

        // Legacy SHA256 hex format
        var legacy = LegacyHashPassword(password);
        return string.Equals(legacy, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string LegacyHashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

