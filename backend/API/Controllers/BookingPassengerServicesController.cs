namespace API.Controllers;

using API.Application.Dtos.Booking;
using API.Application.Exceptions;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v1/bookings/{bookingId:int}/passengers/{passengerId:int}/services")]
[Authorize]
public class BookingPassengerServicesController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingPassengerServicesController> _logger;

    public BookingPassengerServicesController(
        IBookingService bookingService,
        ILogger<BookingPassengerServicesController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PassengerServiceResponse>>> GetPassengerServicesAsync(
        int bookingId,
        int passengerId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        try
        {
            var services = await _bookingService.GetPassengerServicesAsync(bookingId, passengerId, userId);
            return Ok(services);
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
            _logger.LogError(ex, "Error getting passenger services for booking {BookingId}, passenger {PassengerId}", bookingId, passengerId);
            return StatusCode(500, new { message = "An error occurred while retrieving passenger services" });
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PassengerServiceResponse>> AddPassengerServiceAsync(
        int bookingId,
        int passengerId,
        [FromBody] AddPassengerServiceDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        try
        {
            var service = await _bookingService.AddPassengerServiceAsync(bookingId, passengerId, userId, dto);
            return CreatedAtAction(
                nameof(GetPassengerServicesAsync),
                new { bookingId, passengerId },
                service);
        }
        catch (ValidationException ex)
        {
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
            _logger.LogError(ex, "Error adding passenger service for booking {BookingId}, passenger {PassengerId}", bookingId, passengerId);
            return StatusCode(500, new { message = "An error occurred while adding passenger service" });
        }
    }

    [HttpPut("{bookingServiceId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PassengerServiceResponse>> UpdatePassengerServiceAsync(
        int bookingId,
        int passengerId,
        int bookingServiceId,
        [FromBody] UpdatePassengerServiceDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        try
        {
            var service = await _bookingService.UpdatePassengerServiceAsync(
                bookingId,
                passengerId,
                bookingServiceId,
                userId,
                dto);
            return Ok(service);
        }
        catch (ValidationException ex)
        {
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
            _logger.LogError(ex, "Error updating passenger service {BookingServiceId}", bookingServiceId);
            return StatusCode(500, new { message = "An error occurred while updating passenger service" });
        }
    }

    [HttpDelete("{bookingServiceId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePassengerServiceAsync(
        int bookingId,
        int passengerId,
        int bookingServiceId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        try
        {
            await _bookingService.RemovePassengerServiceAsync(bookingId, passengerId, bookingServiceId, userId);
            return Ok(new { message = "Passenger service removed successfully" });
        }
        catch (ValidationException ex)
        {
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
            _logger.LogError(ex, "Error removing passenger service {BookingServiceId}", bookingServiceId);
            return StatusCode(500, new { message = "An error occurred while removing passenger service" });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out userId);
    }
}
