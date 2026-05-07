namespace API.Infrastructure.Security;

using API.Application.Interfaces;
using BCrypt.Net;

public class PasswordHasher : IPasswordHasher
{
    // BCrypt work factor (cost): 11 = 2^11 iterations
    // Higher = more secure but slower
    // 11 is a good balance for 2024
    private const int WorkFactor = 11;

    public string HashPassword(string password)
    {
        // Hash password with BCrypt
        // BCrypt automatically generates a salt and includes it in the hash
        // Format: $2a$11$[22 char salt][31 char hash]
        return BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            // BCrypt.Verify handles both the salt extraction and comparison
            // Returns true if password matches the hash
            return BCrypt.Verify(password, hash);
        }
        catch
        {
            // If hash format is invalid or any error occurs, return false
            return false;
        }
    }
}
