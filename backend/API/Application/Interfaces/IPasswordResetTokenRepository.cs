namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Gets a password reset token by its code.
    /// </summary>
    Task<PasswordResetToken?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets a password reset token by ID.
    /// </summary>
    Task<PasswordResetToken?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new password reset token.
    /// </summary>
    Task<PasswordResetToken> CreateAsync(PasswordResetToken token);

    /// <summary>
    /// Deletes a password reset token by ID.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Gets all reset tokens for a user.
    /// </summary>
    Task<List<PasswordResetToken>> GetByUserIdAsync(int userId);
}
