using FlightBooking.Application.DTOs.Auth;

namespace FlightBooking.Application.Services.Interfaces;

public interface IOAuthService
{
    Task<AuthResponse> AuthenticateWithGoogleAsync(string idToken);
    Task LinkGoogleAccountAsync(Guid userId, string idToken);
    Task UnlinkGoogleAccountAsync(Guid userId);
}
