namespace API.Controllers;

using API.Application.Dtos.Notification;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets user's notification history.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<NotificationResponse>>> GetNotificationsAsync(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Fetching notifications for user {UserId}", userId);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly, page, pageSize);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, new { message = "Error fetching notifications" });
        }
    }

    /// <summary>
    /// Gets user's unread notification count.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetUnreadCountAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// Marks one notification as read.
    /// </summary>
    [HttpPut("{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsReadAsync(int notificationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAsReadAsync(userId, notificationId);
        return success ? Ok(new { message = "Notification marked as read" }) : NotFound();
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> MarkAllAsReadAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var markedCount = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { markedCount });
    }

    /// <summary>
    /// Gets user's notification settings.
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> GetSettingsAsync()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var settings = await _notificationService.GetNotificationSettingsAsync(userId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings");
            return StatusCode(500, new { message = "Error fetching settings" });
        }
    }

    /// <summary>
    /// Updates user's notification settings.
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettingsAsync([FromBody] NotificationSettingsDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Updating notification settings for user {UserId}", userId);
            var success = await _notificationService.UpdateNotificationSettingsAsync(userId, dto);
            return success ? Ok(new { message = "Settings updated" }) : BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            return StatusCode(500, new { message = "Error updating settings" });
        }
    }
}
