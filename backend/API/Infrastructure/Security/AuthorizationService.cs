namespace API.Infrastructure.Security;

using API.Middleware;
using System.Security.Claims;

public interface IAuthorizationService
{
    bool IsUserOwner(ClaimsPrincipal user, int ownerId);
    bool IsAdmin(ClaimsPrincipal user);
    bool HasRole(ClaimsPrincipal user, string role);
    int GetUserId(ClaimsPrincipal user);
    List<string> GetUserRoles(ClaimsPrincipal user);
}

public class AuthorizationService : IAuthorizationService
{
    public bool IsUserOwner(ClaimsPrincipal user, int ownerId)
    {
        var userId = GetUserId(user);
        return userId == ownerId || IsAdmin(user);
    }

    public bool IsAdmin(ClaimsPrincipal user)
    {
        return HasRole(user, "Admin");
    }

    public bool HasRole(ClaimsPrincipal user, string role)
    {
        return user?.FindFirst(ClaimTypes.Role)?.Value?.Contains(role) ?? false;
    }

    public int GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    public List<string> GetUserRoles(ClaimsPrincipal user)
    {
        var rolesClaim = user?.FindFirst(ClaimTypes.Role)?.Value ?? "";
        return rolesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
