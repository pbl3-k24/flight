namespace API.Controllers;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersAdminController : ControllerBase
{
    private readonly IUserAdminService _userService;
    private readonly ILogger<UsersAdminController> _logger;

    public UsersAdminController(
        IUserAdminService userService,
        ILogger<UsersAdminController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users (Admin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserManagementResponse>>> GetUsersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Getting users");
            var response = await _userService.GetUsersAsync(page, pageSize);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "Error getting users" });
        }
    }

    /// <summary>
    /// Gets user details (Admin only).
    /// </summary>
    [HttpGet("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserManagementResponse>> GetUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Getting user: {UserId}", userId);
            var response = await _userService.GetUserAsync(userId);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            return StatusCode(500, new { message = "Error getting user" });
        }
    }

    /// <summary>
    /// Updates user status (Admin only).
    /// </summary>
    [HttpPut("{userId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatusAsync(int userId, [FromBody] UpdateUserStatusDto dto)
    {
        try
        {
            _logger.LogInformation("Updating user status: {UserId}", userId);
            var success = await _userService.UpdateUserStatusAsync(userId, dto);
            return success ? Ok(new { message = "User status updated successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status");
            return StatusCode(500, new { message = "Error updating user status" });
        }
    }

    /// <summary>
    /// Assigns a role to a user (Admin only).
    /// </summary>
    [HttpPost("{userId}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoleAsync(int userId, [FromBody] AssignRoleDto dto)
    {
        try
        {
            _logger.LogInformation("Assigning role to user: {UserId}", userId);
            var success = await _userService.AssignRoleAsync(userId, dto);
            return success ? Ok(new { message = "Role assigned successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return StatusCode(500, new { message = "Error assigning role" });
        }
    }

    /// <summary>
    /// Removes a role from a user (Admin only).
    /// </summary>
    [HttpDelete("{userId}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleAsync(int userId, int roleId)
    {
        try
        {
            _logger.LogInformation("Removing role from user: {UserId}", userId);
            var success = await _userService.RemoveRoleAsync(userId, roleId);
            return success ? Ok(new { message = "Role removed successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return StatusCode(500, new { message = "Error removing role" });
        }
    }

    /// <summary>
    /// Gets user booking history (Admin only).
    /// </summary>
    [HttpGet("{userId}/bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingManagementResponse>>> GetUserBookingsAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Getting user bookings: {UserId}", userId);
            var response = await _userService.GetUserBookingsAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user bookings");
            return StatusCode(500, new { message = "Error getting user bookings" });
        }
    }
}
