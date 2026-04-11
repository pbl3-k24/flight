using FlightBooking.Application.DTOs.Auth;

namespace FlightBooking.Application.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task<AuthResponse> LoginWithGoogleAsync(string idToken);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
