using System.Security.Cryptography;
using DiemEcommerce.Application.Abstractions;

namespace DiemEcommerce.Infrastructure.PasswordHasher;

public class PasswordHasherService : IPasswordHasherService
{
    private const int SaltSize = 16; // 128 bit 
    private const int KeySize = 32; // 256 bit
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName HashAlgorithmName = HashAlgorithmName.SHA256;

    public  string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName,
            KeySize);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public  bool VerifyPassword(string password, string hashedPassword)
    {
        string[] parts = hashedPassword.Split(':');
        if (parts.Length != 2)
            return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] storedHash = Convert.FromBase64String(parts[1]);

        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}