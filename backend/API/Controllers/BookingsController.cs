namespace API.Controllers;

using API.Application.Dtos.Booking;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IFlightService _flightService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        IBookingService bookingService,
        IFlightService flightService,
        ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _flightService = flightService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking.
    /// </summary>
    /// <param name="dto">Booking details</param>
    /// <returns>Created booking information</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookingResponse>> CreateBookingAsync([FromBody] CreateBookingDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            _logger.LogInformation("Booking creation requested by user {UserId}", userId);

            var booking = await _bookingService.CreateBookingAsync(userId, dto);
            return CreatedAtRoute(nameof(GetBookingAsync), new { id = booking.BookingId }, booking);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Booking validation error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Booking creation error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, new { message = "An error occurred while creating booking" });
        }
    }

    /// <summary>
    /// Gets a booking by ID.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details</returns>
    [HttpGet("{id}", Name = nameof(GetBookingAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookingResponse>> GetBookingAsync(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            _logger.LogInformation("Booking requested by user {UserId}: {BookingId}", userId, id);

            var booking = await _bookingService.GetBookingAsync(id, userId);
            return Ok(booking);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking {BookingId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving booking" });
        }
    }

    /// <summary>
    /// Gets all bookings for the authenticated user.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>List of user's bookings</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<BookingResponse>>> GetUserBookingsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            _logger.LogInformation("Bookings requested by user {UserId}", userId);

            var bookings = await _bookingService.GetUserBookingsAsync(userId, page, pageSize);
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user bookings");
            return StatusCode(500, new { message = "An error occurred while retrieving bookings" });
        }
    }

    /// <summary>
    /// Updates a booking.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="dto">Updated booking information</param>
    /// <returns>Success indicator</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateBookingAsync(int id, [FromBody] UpdateBookingDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            _logger.LogInformation("Booking update requested by user {UserId}: {BookingId}", userId, id);

            var success = await _bookingService.UpdateBookingAsync(id, userId, dto);
            return success ? Ok(new { message = "Booking updated successfully" }) : BadRequest();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Booking update validation error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking {BookingId}", id);
            return StatusCode(500, new { message = "An error occurred while updating booking" });
        }
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="cancellationRequest">Cancellation details</param>
    /// <returns>Success indicator</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelBookingAsync(int id, [FromBody] CancellationRequest cancellationRequest)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            _logger.LogInformation("Booking cancellation requested by user {UserId}: {BookingId}", userId, id);

            var success = await _bookingService.CancelBookingAsync(id, userId, cancellationRequest.Reason ?? "Customer request");
            return success ? Ok(new { message = "Booking cancelled successfully" }) : Conflict(new { message = "Cannot cancel this booking" });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Cancellation validation error: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling booking" });
        }
    }
}

public class CancellationRequest
{
    public string? Reason { get; set; }
}
