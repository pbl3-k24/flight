using Microsoft.AspNetCore.Authentication;

namespace API.Middleware;

/// <summary>
/// Middleware to explicitly authenticate Bearer JWT tokens.
/// Keeps authentication behavior but avoids logging sensitive header data.
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
            var authHeader = context.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var authResult = await context.AuthenticateAsync("Bearer");
                if (authResult.Succeeded && authResult.Principal != null)
                {
                    context.User = authResult.Principal;
                }
                else
                {
                    _logger.LogWarning("Bearer token authentication failed: {Failure}",
                        authResult.Failure?.Message ?? "Unknown error");
                }
            }
            else if (!string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("Authorization header present but not Bearer format");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in JwtAuthenticationMiddleware");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method for JwtAuthenticationMiddleware.
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthenticationMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}
