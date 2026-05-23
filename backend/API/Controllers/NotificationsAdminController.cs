namespace API.Controllers;

using API.Application.Dtos.Notification;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/notifications")]
[Authorize(Roles = "Admin")]
public class NotificationsAdminController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsAdminController> _logger;

    public NotificationsAdminController(
        INotificationService notificationService,
        ILogger<NotificationsAdminController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Broadcasts a notification to all active users (Admin only).
    /// </summary>
    [HttpPost("broadcast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BroadcastNotificationResponse>> BroadcastAsync([FromBody] BroadcastNotificationDto dto)
    {
        if (dto == null
            || string.IsNullOrWhiteSpace(dto.Subject)
            || string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new { message = "Subject and message are required" });
        }

        try
        {
            var normalizedDto = new BroadcastNotificationDto
            {
                Subject = dto.Subject.Trim(),
                Message = dto.Message.Trim(),
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "SYSTEM" : dto.Category.Trim(),
                SendEmail = dto.SendEmail
            };

            var result = await _notificationService.BroadcastNotificationAsync(normalizedDto);
            _logger.LogInformation(
                "Admin broadcast completed. Total={Total}, Success={Success}, Failed={Failed}",
                result.TotalUsers,
                result.SuccessCount,
                result.FailedCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting system notification");
            return StatusCode(500, new { message = "Error broadcasting notification" });
        }
    }
}
