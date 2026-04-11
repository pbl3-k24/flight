using FlightBooking.Application.DTOs.Auth;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class OAuthService(IUserRepository userRepository, IRoleRepository roleRepository) : IOAuthService
{
    public Task<AuthResponse> AuthenticateWithGoogleAsync(string idToken)
    {
        // In production: call Google's tokeninfo endpoint or use Google.Apis.Auth to validate idToken
        // Extract email, googleUserId from the token, then find or create user
        throw new NotImplementedException("Google OAuth requires Google.Apis.Auth library integration.");
    }

    public Task LinkGoogleAccountAsync(Guid userId, string idToken)
    {
        throw new NotImplementedException("Google OAuth requires Google.Apis.Auth library integration.");
    }

    public Task UnlinkGoogleAccountAsync(Guid userId)
    {
        throw new NotImplementedException("Google OAuth requires Google.Apis.Auth library integration.");
    }
}
