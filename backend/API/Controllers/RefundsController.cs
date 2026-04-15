namespace API.Controllers;

using API.Application.Dtos.Refund;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RefundsController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ILogger<RefundsController> _logger;

    public RefundsController(
        IRefundService refundService,
        ILogger<RefundsController> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Requests a refund for a booking.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RefundResponse>> RequestRefundAsync([FromBody] RefundRequest dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            _logger.LogInformation("Refund requested by user {UserId} for booking {BookingId}",
                userId, dto.BookingId);

            var response = await _refundService.RequestRefundAsync(dto.BookingId, userId, dto.Reason);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting refund");
            return StatusCode(500, new { message = "An error occurred while requesting refund" });
        }
    }

    /// <summary>
    /// Gets refund status.
    /// </summary>
    [HttpGet("{refundId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefundResponse>> GetRefundStatusAsync(int refundId)
    {
        try
        {
            var response = await _refundService.GetRefundStatusAsync(refundId);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund status");
            return StatusCode(500, new { message = "An error occurred while retrieving refund status" });
        }
    }

    /// <summary>
    /// Gets refund history for a booking.
    /// </summary>
    [HttpGet("booking/{bookingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RefundResponse>>> GetRefundHistoryAsync(int bookingId)
    {
        try
        {
            var response = await _refundService.GetRefundHistoryAsync(bookingId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund history");
            return StatusCode(500, new { message = "An error occurred while retrieving refund history" });
        }
    }
}
