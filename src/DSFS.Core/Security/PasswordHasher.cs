using System.Security.Cryptography;

namespace DSFS.Core.Security;

/// <summary>
/// Salted PBKDF2 password hashing for the User Interface / Registration Module (section 5.4.2.1).
/// Uses only the .NET base class library (System.Security.Cryptography) - no external dependency.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;

    public static (string hashBase64, string saltBase64) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, string hashBase64, string saltBase64)
    {
        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] expected = Convert.FromBase64String(hashBase64);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
