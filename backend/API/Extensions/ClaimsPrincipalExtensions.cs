namespace API.Extensions;

using API.Application.Exceptions;
using System.Security.Claims;

/// <summary>
/// Extension methods for claim parsing from HttpContext.User
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from claims with safe parsing
    /// </summary>
    /// <param name="principal">The claims principal (usually HttpContext.User)</param>
    /// <param name="userId">The parsed user ID</param>
    /// <returns>True if user ID was found and parsed, false otherwise</returns>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId)
    {
        userId = 0;
        var claim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(claim))
            return false;

        return int.TryParse(claim, out userId);
    }

    /// <summary>
    /// Gets the user ID from claims or throws UnauthorizedException
    /// </summary>
    /// <param name="principal">The claims principal (usually HttpContext.User)</param>
    /// <returns>The parsed user ID</returns>
    /// <exception cref="UnauthorizedException">Thrown if user ID cannot be obtained or parsed</exception>
    public static int GetUserIdOrThrow(this ClaimsPrincipal principal)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            throw new UnauthorizedException("User context is invalid or missing");
        }

        return userId;
    }

    /// <summary>
    /// Gets the user email from claims
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the user name from claims
    /// </summary>
    public static string? GetName(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Name)?.Value;
    }
}
