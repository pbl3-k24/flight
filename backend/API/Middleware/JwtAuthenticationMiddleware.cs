using Microsoft.AspNetCore.Authentication;

namespace API.Middleware;

/// <summary>
/// Middleware to explicitly authenticate Bearer JWT tokens
/// This extracts and validates Bearer tokens from every request
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        try
        {
            _logger.LogInformation("=== JwtAuthenticationMiddleware Start ===");
            _logger.LogInformation("Request to {Path} {Method}", context.Request.Path, context.Request.Method);

            // Log all headers để debug
            _logger.LogInformation("Headers count: {Count}", context.Request.Headers.Count);
            foreach (var header in context.Request.Headers)
            {
                _logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value.ToString());
            }

            var authHeader = context.Request.Headers.Authorization.ToString();
            _logger.LogInformation("Authorization header value: '{AuthHeader}' (IsNullOrEmpty: {IsNullOrEmpty})", 
                authHeader, string.IsNullOrEmpty(authHeader));
            _logger.LogInformation("Authorization header type: {Type}", authHeader?.GetType().Name ?? "null");

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("✓ Bearer token found in Authorization header");

                // Try to authenticate with Bearer scheme
                var authResult = await context.AuthenticateAsync("Bearer");

                _logger.LogInformation("Authentication result - Succeeded: {Succeeded}, Principal: {HasPrincipal}", 
                    authResult.Succeeded, authResult.Principal != null);

                if (authResult.Succeeded && authResult.Principal != null)
                {
                    _logger.LogInformation("✓ Bearer token authenticated successfully. Setting HttpContext.User");
                    context.User = authResult.Principal;
                    _logger.LogInformation("User set - Identity: {Identity}, IsAuthenticated: {IsAuthenticated}", 
                        context.User.Identity?.Name, context.User.Identity?.IsAuthenticated);
                }
                else
                {
                    _logger.LogWarning("✗ Bearer token authentication failed: {Failure}", 
                        authResult.Failure?.Message ?? "Unknown error");
                }
            }
            else if (!string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("✗ Authorization header present but not Bearer format: '{Header}'", authHeader);
            }
            else
            {
                _logger.LogInformation("✗ No Authorization header found");
            }

            _logger.LogInformation("=== JwtAuthenticationMiddleware End ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in JwtAuthenticationMiddleware");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method for JwtAuthenticationMiddleware
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthenticationMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}
