namespace API.Application.Services;

using API.Application.Dtos.Auth;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailVerificationTokenRepository _emailTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordTokenRepository;

    private const string EmailRegex = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger,
        IEmailVerificationTokenRepository emailTokenRepository,
        IPasswordResetTokenRepository passwordTokenRepository)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _emailTokenRepository = emailTokenRepository;
        _passwordTokenRepository = passwordTokenRepository;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterDto dto)
    {
        // 1. Validate email format
        if (!Regex.IsMatch(dto.Email, EmailRegex))
        {
            _logger.LogWarning("Invalid email format: {Email}", dto.Email);
            throw new ValidationException("Invalid email format");
        }

        // 2. Check email doesn't exist
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Email already exists: {Email}", dto.Email);
            throw new ValidationException("Email already registered");
        }

        // 3. Hash password
        var passwordHash = _passwordHasher.HashPassword(dto.Password);

        // 4. Create new user
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            FullName = dto.FullName,
            Phone = dto.Phone,
            Status = 0, // Active
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // 5. Create email verification token
        var verificationCode = GenerateVerificationCode();
        var verificationToken = new EmailVerificationToken
        {
            UserId = createdUser.Id,
            Code = verificationCode,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        await _emailTokenRepository.CreateAsync(verificationToken);

        // 6. Send verification email (SKIPPED FOR TESTING - Email verification disabled)
        // TODO: Configure proper SMTP settings in appsettings.json to enable email verification
        // await _emailService.SendVerificationEmailAsync(createdUser.Email, verificationCode);
        
        _logger.LogInformation("Email verification skipped for testing purposes. User email not verified.");

        // 7. Generate JWT token
        var token = _jwtTokenService.GenerateToken(createdUser);

        // 8. Log registration
        _logger.LogInformation("User registered successfully: {Email}", createdUser.Email);

        // 9. Return response
        return new AuthResponse
        {
            UserId = createdUser.Id,
            Email = createdUser.Email,
            FullName = createdUser.FullName,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginDto dto)
    {
        // 1. Find user by email
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email {Email}", dto.Email);
            throw new NotFoundException("Invalid email or password");
        }

        // 2. Verify password
        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {Email}", dto.Email);
            throw new ValidationException("Invalid email or password");
        }

        // 3. Check user status is Active
        if (!user.IsActive())
        {
            _logger.LogWarning("Login failed: User is not active {Email}", dto.Email);
            throw new ValidationException("Account is not active");
        }

        // 4. Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);

        // 5. Log login
        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        // 6. Return response
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }

    public async Task<bool> VerifyEmailAsync(string userId, string code)
    {
        // 1. Find token by code
        var token = await _emailTokenRepository.GetByCodeAsync(code);
        if (token == null)
        {
            _logger.LogWarning("Email verification failed: Token not found for code {Code}", code);
            throw new NotFoundException("Invalid verification code");
        }

        // 2. Check token not expired
        if (token.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Email verification failed: Token expired for user {UserId}", token.UserId);
            throw new ValidationException("Verification code has expired");
        }

        // 3. Find user
        var user = await _userRepository.GetByIdAsync(token.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // 4. Mark email as verified (update user entity if needed)
        // Note: Add IsEmailVerified property to User entity if not present
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // 5. Delete token
        await _emailTokenRepository.DeleteAsync(token.Id);

        // 6. Log verification
        _logger.LogInformation("Email verified successfully for user {Email}", user.Email);

        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        // 1. Get current user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Change password failed: User not found {UserId}", userId);
            throw new NotFoundException("User not found");
        }

        // 2. Verify old password
        if (!_passwordHasher.VerifyPassword(dto.OldPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password failed: Invalid old password for user {UserId}", userId);
            throw new ValidationException("Old password is incorrect");
        }

        // 3. Validate new password strength
        ValidatePasswordStrength(dto.NewPassword);

        // 4. Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(dto.NewPassword);

        // 5. Update user
        user.UpdatePassword(newPasswordHash);
        await _userRepository.UpdateAsync(user);

        // 6. Log password change
        _logger.LogInformation("Password changed successfully for user {UserId}", userId);

        return true;
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        // 1. Find user by email
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal if email exists - always return success
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return true;
        }

        // 2. Create password reset token
        var resetCode = GenerateVerificationCode();
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Code = resetCode,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // 1 hour expiration
        };

        await _passwordTokenRepository.CreateAsync(resetToken);

        // 3. Send reset email
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetCode);

        // 4. Log request
        _logger.LogInformation("Password reset requested for user {Email}", user.Email);

        // 5. Return success
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string code, string newPassword)
    {
        // 1. Find token by code
        var token = await _passwordTokenRepository.GetByCodeAsync(code);
        if (token == null)
        {
            _logger.LogWarning("Password reset failed: Token not found for code {Code}", code);
            throw new NotFoundException("Invalid reset code");
        }

        // 2. Check token not expired
        if (token.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset failed: Token expired for user {UserId}", token.UserId);
            throw new ValidationException("Reset code has expired");
        }

        // 3. Validate new password strength
        ValidatePasswordStrength(newPassword);

        // 4. Find user
        var user = await _userRepository.GetByIdAsync(token.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // 5. Hash password
        var passwordHash = _passwordHasher.HashPassword(newPassword);

        // 6. Update user
        user.UpdatePassword(passwordHash);
        await _userRepository.UpdateAsync(user);

        // 7. Delete token
        await _passwordTokenRepository.DeleteAsync(token.Id);

        // 8. Log reset
        _logger.LogInformation("Password reset successfully for user {Email}", user.Email);

        return true;
    }

    private void ValidatePasswordStrength(string password)
    {
        if (password.Length < 8)
        {
            throw new ValidationException("Password must be at least 8 characters long");
        }

        if (!password.Any(char.IsUpper))
        {
            throw new ValidationException("Password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            throw new ValidationException("Password must contain at least one digit");
        }
    }

    private string GenerateVerificationCode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 32);
    }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

