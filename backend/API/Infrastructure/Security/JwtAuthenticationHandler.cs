namespace API.Infrastructure.Security;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using API.Application.Interfaces;

/// <summary>
/// Custom authentication handler that validates JWT tokens from Authorization header
/// Works with the existing JwtTokenService without requiring external JWT packages
/// </summary>
public class JwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<JwtAuthenticationHandler> _logger;

    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IJwtTokenService jwtTokenService,
        ILogger<JwtAuthenticationHandler> logger)
        : base(options, loggerFactory, encoder)
    {
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Extract token from Authorization header
            var authHeader = Request.Headers.Authorization.ToString();
            _logger.LogInformation("Authorization Header: {AuthHeader}", authHeader);

            if (string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("No Authorization header found");
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid authorization header format: {AuthHeader}", authHeader);
                return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header format"));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            _logger.LogInformation("Extracted token (first 50 chars): {Token}...", token.Substring(0, Math.Min(50, token.Length)));

            // Validate token using JwtTokenService
            var principal = _jwtTokenService.ValidateToken(token);

            if (principal == null)
            {
                _logger.LogWarning("Invalid or expired token");
                return Task.FromResult(AuthenticateResult.Fail("Invalid or expired token"));
            }

            _logger.LogInformation("Token validated successfully. Principal claims: {Claims}", 
                string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}")));

            // Create authentication ticket
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating request: {Message}", ex.Message);
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed"));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        return Response.WriteAsJsonAsync(new { error = "Unauthorized: Valid JWT token required" });
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        return Response.WriteAsJsonAsync(new { error = "Forbidden: You don't have permission to access this resource" });
    }
}

