namespace API.Application.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration config, ILogger<JwtTokenService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        var secretKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        var expirationHours = int.TryParse(_config["Jwt:ExpirationHours"], out var hours) ? hours : 24;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        // Add role claims from user's roles (via UserRoles join table)
        if (user.UserRoles != null && user.UserRoles.Count > 0)
        {
            _logger.LogInformation("User {UserId} has {RoleCount} roles", user.Id, user.UserRoles.Count);
            foreach (var userRole in user.UserRoles)
            {
                _logger.LogInformation("Adding role claim: {RoleName}", userRole.Role.Name);
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
            }
        }
        else
        {
            _logger.LogWarning("User {UserId} has NO roles! UserRoles is {RolesStatus}", 
                user.Id, user.UserRoles == null ? "null" : "empty");
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            SigningCredentials = credentials,
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("JWT token generated for user {UserId} with {ClaimCount} claims: {Claims}", 
            user.Id, claims.Count, string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

        return tokenString;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            _logger.LogInformation("Validating JWT token (first 50 chars): {Token}...", 
                token.Substring(0, Math.Min(50, token.Length)));

            var secretKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            _logger.LogInformation("ValidateIssuer: {ValidateIssuer}, Issuer: {Issuer}", 
                !string.IsNullOrEmpty(_config["Jwt:Issuer"]), _config["Jwt:Issuer"]);
            _logger.LogInformation("ValidateAudience: {ValidateAudience}, Audience: {Audience}", 
                !string.IsNullOrEmpty(_config["Jwt:Audience"]), _config["Jwt:Audience"]);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = !string.IsNullOrEmpty(_config["Jwt:Issuer"]),
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = !string.IsNullOrEmpty(_config["Jwt:Audience"]),
                ValidAudience = _config["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(60)
            }, out SecurityToken validatedToken);

            _logger.LogInformation("JWT token validated successfully. Claims: {Claims}", 
                string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}")));
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("JWT token validation failed: {Message}", ex.Message);
            return null;
        }
    }
}
