namespace API.Infrastructure.Security;

using API.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        // Generate salt
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

        // Hash password with PBKDF2
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            Algorithm,
            HashSize);

        var hashWithSalt = new byte[SaltSize + HashSize];
        Array.Copy(saltBytes, 0, hashWithSalt, 0, SaltSize);
        Array.Copy(hash, 0, hashWithSalt, SaltSize, HashSize);

        return Convert.ToBase64String(hashWithSalt);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hash);

            if (hashBytes.Length != SaltSize + HashSize)
            {
                return false;
            }

            var saltBytes = new byte[SaltSize];
            Array.Copy(hashBytes, 0, saltBytes, 0, SaltSize);

            byte[] hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                Algorithm,
                HashSize);

            for (int i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hashToCompare[i])
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
