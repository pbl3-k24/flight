namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IEmailVerificationTokenRepository
{
    /// <summary>
    /// Gets an email verification token by its code.
    /// </summary>
    Task<EmailVerificationToken?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets an email verification token by ID.
    /// </summary>
    Task<EmailVerificationToken?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new email verification token.
    /// </summary>
    Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token);

    /// <summary>
    /// Deletes an email verification token by ID.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Gets all verification tokens for a user.
    /// </summary>
    Task<List<EmailVerificationToken>> GetByUserIdAsync(int userId);
}
