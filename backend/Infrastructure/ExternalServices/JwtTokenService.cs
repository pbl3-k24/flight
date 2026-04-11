using FlightBooking.Domain.Interfaces.Services;
using FlightBooking.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FlightBooking.Infrastructure.ExternalServices;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;

    // In production: store refresh tokens in Redis/DB. Here we use a simple in-memory set for demo.
    private static readonly HashSet<string> _revokedRefreshTokens = [];

    public JwtTokenService(IConfiguration config)
    {
        _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _issuer = config["Jwt:Issuer"] ?? "FlightBooking";
        _audience = config["Jwt:Audience"] ?? "FlightBookingClient";
        _accessTokenMinutes = int.TryParse(config["Jwt:AccessTokenMinutes"], out var atm) ? atm : 15;
        _refreshTokenDays = int.TryParse(config["Jwt:RefreshTokenDays"], out var rtd) ? rtd : 30;
    }

    public (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokens(User user)
    {
        var roles = user.UserRoles?.Select(ur => ur.Role?.Name ?? string.Empty) ?? [];
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);

        var accessToken = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        // Refresh token is a long-lived JWT with minimal claims
        var refreshClaims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()) };
        var refreshToken = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: refreshClaims,
            expires: DateTime.UtcNow.AddDays(_refreshTokenDays),
            signingCredentials: creds);

        return (
            new JwtSecurityTokenHandler().WriteToken(accessToken),
            new JwtSecurityTokenHandler().WriteToken(refreshToken),
            expiresAt);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        if (_revokedRefreshTokens.Contains(refreshToken)) return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true
            }, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public void RevokeRefreshToken(string refreshToken)
        => _revokedRefreshTokens.Add(refreshToken);
}
