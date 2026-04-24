namespace API.Application.Interfaces;

public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using a secure hashing algorithm.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">Hashed password</param>
    /// <returns>True if password matches</returns>
    bool VerifyPassword(string password, string hashedPassword);
}