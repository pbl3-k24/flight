namespace API.Application.Interfaces;

public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using PBKDF2 with SHA256.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hash">Password hash</param>
    /// <returns>True if password matches hash</returns>
    bool VerifyPassword(string password, string hash);
}
