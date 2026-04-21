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
public class BookingsAdminController : ControllerBase
{
    private readonly IBookingAdminService _bookingService;
    private readonly ILogger<BookingsAdminController> _logger;

    public BookingsAdminController(
        IBookingAdminService bookingService,
        ILogger<BookingsAdminController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Searches bookings with filters (Admin only).
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingManagementResponse>>> SearchBookingsAsync([FromBody] BookingSearchFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Searching bookings with filter");
            var response = await _bookingService.SearchBookingsAsync(filter);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching bookings");
            return StatusCode(500, new { message = "Error searching bookings" });
        }
    }

    /// <summary>
    /// Gets booking details (Admin only).
    /// </summary>
    [HttpGet("{bookingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingManagementResponse>> GetBookingAsync(int bookingId)
    {
        try
        {
            _logger.LogInformation("Getting booking: {BookingId}", bookingId);
            var response = await _bookingService.GetBookingAsync(bookingId);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking");
            return StatusCode(500, new { message = "Error getting booking" });
        }
    }

    /// <summary>
    /// Cancels a booking (Admin override) (Admin only).
    /// </summary>
    [HttpDelete("{bookingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBookingAsync(int bookingId, [FromBody] CancelBookingAdminDto dto)
    {
        try
        {
            _logger.LogInformation("Admin cancelling booking: {BookingId}", bookingId);
            var success = await _bookingService.CancelBookingAsync(bookingId, dto);
            return success ? Ok(new { message = "Booking cancelled successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking");
            return StatusCode(500, new { message = "Error cancelling booking" });
        }
    }

    /// <summary>
    /// Gets pending refunds (Admin only).
    /// </summary>
    [HttpGet("refunds/pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RefundManagementResponse>>> GetPendingRefundsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Getting pending refunds");
            var response = await _bookingService.GetPendingRefundsAsync(page, pageSize);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending refunds");
            return StatusCode(500, new { message = "Error getting pending refunds" });
        }
    }

    /// <summary>
    /// Gets refund details (Admin only).
    /// </summary>
    [HttpGet("refunds/{refundId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefundManagementResponse>> GetRefundAsync(int refundId)
    {
        try
        {
            _logger.LogInformation("Getting refund: {RefundId}", refundId);
            var response = await _bookingService.GetRefundAsync(refundId);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund");
            return StatusCode(500, new { message = "Error getting refund" });
        }
    }

    /// <summary>
    /// Approves or rejects a refund (Admin only).
    /// </summary>
    [HttpPost("refunds/{refundId}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRefundAsync(int refundId, [FromBody] ApproveRefundDto dto)
    {
        try
        {
            _logger.LogInformation("Admin approving refund: {RefundId}", refundId);
            var success = await _bookingService.ApproveRefundAsync(refundId, dto);
            return success ? Ok(new { message = "Refund processed successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving refund");
            return StatusCode(500, new { message = "Error approving refund" });
        }
    }
}
