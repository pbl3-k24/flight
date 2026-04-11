namespace FlightBooking.Domain.Interfaces.Services;

public interface IJwtTokenService
{
    (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokens(Entities.User user);
    System.Security.Claims.ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
    void RevokeRefreshToken(string refreshToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
