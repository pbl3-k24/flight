using FlightBooking.Application.DTOs.Auth;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Domain.Interfaces.Services;

namespace FlightBooking.Application.Services.Implementations;

public class AuthService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher,
    IVerificationService verificationService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email))
            throw new InvalidOperationException("Email already registered.");

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Phone = request.Phone,
            Status = "active",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var userRole = await roleRepository.GetByNameAsync("user")
            ?? throw new InvalidOperationException("Default role 'user' not found.");

        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id, AssignedAt = DateTime.UtcNow });

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        await verificationService.SendEmailOtpAsync(user.Id, "email_verification");

        var (accessToken, refreshToken, expiresAt) = jwtTokenService.GenerateTokens(user);
        return BuildResponse(accessToken, refreshToken, expiresAt, user, ["user"]);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.PasswordHash is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.Status != "active")
            throw new InvalidOperationException($"Account is {user.Status}.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (accessToken, refreshToken, expiresAt) = jwtTokenService.GenerateTokens(user);
        return BuildResponse(accessToken, refreshToken, expiresAt, user, roles);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var principal = jwtTokenService.ValidateRefreshToken(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var userId = Guid.Parse(principal.FindFirst("sub")!.Value);
        var user = await userRepository.GetByIdWithRolesAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (newAccess, newRefresh, expiresAt) = jwtTokenService.GenerateTokens(user);
        return BuildResponse(newAccess, newRefresh, expiresAt, user, roles);
    }

    public Task RevokeTokenAsync(string refreshToken)
    {
        jwtTokenService.RevokeRefreshToken(refreshToken);
        return Task.CompletedTask;
    }

    public async Task<AuthResponse> LoginWithGoogleAsync(string idToken)
    {
        // Delegated to OAuthService
        throw new NotImplementedException("Use IOAuthService.AuthenticateWithGoogleAsync instead.");
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await userRepository.GetByEmailAsync(email);
        if (user is null) return; // Do not reveal whether email exists
        await verificationService.SendEmailOtpAsync(user.Id, "password_reset");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");

        var valid = await verificationService.VerifyEmailOtpAsync(user.Id, request.OtpCode, "password_reset");
        if (!valid) throw new InvalidOperationException("Invalid or expired OTP code.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.PasswordHash is null || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync();
    }

    private static AuthResponse BuildResponse(string accessToken, string refreshToken, DateTime expiresAt, User user, IEnumerable<string> roles)
    {
        var dto = new UserDto(
            user.Id, user.Email, user.Phone, user.Status, user.EmailVerified,
            user.Profile is null ? null : new UserProfileDto(
                user.Profile.FullName, user.Profile.DateOfBirth, user.Profile.Gender,
                user.Profile.Nationality, user.Profile.IdentityNumber, user.Profile.PassportNumber, user.Profile.AvatarUrl),
            roles);
        return new AuthResponse(accessToken, refreshToken, expiresAt, dto);
    }
}
