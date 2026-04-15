namespace API.Application.Interfaces;

using API.Application.Dtos.Auth;

public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the provided information.
    /// </summary>
    /// <param name="dto">Registration data containing email, password, full name, and phone</param>
    /// <returns>AuthResponse with user info and verification token</returns>
    Task<AuthResponse> RegisterAsync(RegisterDto dto);

    /// <summary>
    /// Authenticates a user and generates a JWT token.
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <returns>AuthResponse with JWT token and user info</returns>
    Task<AuthResponse> LoginAsync(LoginDto dto);

    /// <summary>
    /// Verifies the user's email address with the provided verification code.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="code">Email verification code</param>
    /// <returns>True if verification successful, false otherwise</returns>
    Task<bool> VerifyEmailAsync(string userId, string code);

    /// <summary>
    /// Changes the password for the authenticated user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="dto">Contains old and new passwords</param>
    /// <returns>True if password change successful, false otherwise</returns>
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);

    /// <summary>
    /// Initiates a password reset process by sending a reset email.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>True if reset email sent successfully, false otherwise</returns>
    Task<bool> RequestPasswordResetAsync(string email);

    /// <summary>
    /// Resets the password using the reset token sent via email.
    /// </summary>
    /// <param name="code">Password reset token</param>
    /// <param name="newPassword">The new password</param>
    /// <returns>True if password reset successful, false otherwise</returns>
    Task<bool> ResetPasswordAsync(string code, string newPassword);
}
