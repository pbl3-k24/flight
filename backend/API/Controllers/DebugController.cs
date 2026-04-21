using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Application.Interfaces;
using API.Infrastructure.Data;

namespace API.Controllers;

/// <summary>
/// Debug controller for testing authentication and authorization
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ILogger<DebugController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly FlightBookingDbContext _dbContext;

    public DebugController(
        ILogger<DebugController> logger,
        IUserRepository userRepository,
        FlightBookingDbContext dbContext)
    {
        _logger = logger;
        _userRepository = userRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Test endpoint - no authentication required
    /// </summary>
    [HttpGet("no-auth")]
    public IActionResult NoAuth()
    {
        return Ok(new { message = "No authentication required", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Debug endpoint - Check admin user in database with roles
    /// </summary>
    [HttpGet("check-admin-roles")]
    public async Task<IActionResult> CheckAdminRoles()
    {
        try
        {
            // Get admin user WITH roles
            var adminUser = await _userRepository.GetByEmailWithRolesAsync("admin@flightbooking.vn");

            if (adminUser == null)
            {
                return Ok(new 
                { 
                    status = "ERROR",
                    message = "Admin user not found in database",
                    timestamp = DateTime.UtcNow
                });
            }

            var rolesInfo = adminUser.Roles?.Select(r => new { r.Id, r.Name }).ToList() ?? new();

            _logger.LogInformation("Admin user found: {UserId}, Roles count: {Count}", adminUser.Id, adminUser.Roles?.Count ?? 0);

            return Ok(new
            {
                status = "OK",
                user = new
                {
                    id = adminUser.Id,
                    email = adminUser.Email,
                    fullName = adminUser.FullName,
                    rolesCount = adminUser.Roles?.Count ?? 0,
                    roles = rolesInfo,
                    rolesDirectCheck = adminUser.Roles?.Count > 0 ? adminUser.Roles.Select(r => r.Name).ToList() : new List<string>()
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin roles");
            return Ok(new 
            { 
                status = "ERROR",
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Debug endpoint - Check all UserRoles in database
    /// </summary>
    [HttpGet("check-all-user-roles")]
    public IActionResult CheckAllUserRoles()
    {
        try
        {
            var userRoles = _dbContext.UserRoles
                .Select(ur => new
                {
                    ur.UserId,
                    ur.RoleId,
                    ur.Role.Name,
                    ur.User.Email
                })
                .ToList();

            var roles = _dbContext.Roles.Select(r => new { r.Id, r.Name }).ToList();
            var users = _dbContext.Users.Select(u => new { u.Id, u.Email, u.FullName }).ToList();

            return Ok(new
            {
                rolesCount = roles.Count,
                roles = roles,
                usersCount = users.Count,
                users = users,
                userRolesCount = userRoles.Count,
                userRoles = userRoles,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user roles");
            return Ok(new 
            { 
                status = "ERROR",
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Test endpoint - requires valid JWT token
    /// </summary>
    [Authorize]
    [HttpGet("with-auth")]
    public IActionResult WithAuth()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role);

        _logger.LogInformation("WithAuth endpoint called. UserId: {UserId}, Email: {Email}, Roles: {Roles}", 
            userId, email, string.Join(",", roles.Select(r => r.Value)));

        return Ok(new 
        { 
            message = "Authentication successful",
            userId,
            email,
            roles = roles.Select(r => r.Value).ToList(),
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint - requires Admin role
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        _logger.LogInformation("AdminOnly endpoint called by UserId: {UserId}, Email: {Email}", userId, email);

        return Ok(new 
        { 
            message = "Admin access granted",
            userId,
            email,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Debug endpoint - shows all claims in current token
    /// </summary>
    [Authorize]
    [HttpGet("claims")]
    public IActionResult ShowClaims()
    {
        var claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList();

        _logger.LogInformation("Claims endpoint called. Total claims: {Count}", claims.Count);

        return Ok(new
        {
            message = "Current claims in token",
            claimsCount = claims.Count,
            claims,
            timestamp = DateTime.UtcNow
        });
    }
}
