namespace API.Controllers;

using Microsoft.AspNetCore.Mvc;
using API.Application.DTOs;
using API.Application.Interfaces;
using API.Domain.Exceptions;

/// <summary>
/// Controller for managing flight bookings.
/// Handles booking creation, retrieval, cancellation, and check-in operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all bookings with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based). Default: 1</param>
    /// <param name="pageSize">Number of items per page. Default: 10, Max: 100</param>
    /// <returns>200 OK with paginated list of bookings</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedBookingsResponseDto>> GetAll(int page = 1, int pageSize = 10)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1)
            {
                _logger.LogWarning("Invalid page number: {Page}", page);
                return BadRequest(new { message = "Page number must be greater than 0." });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                _logger.LogWarning("Invalid page size: {PageSize}", pageSize);
                return BadRequest(new { message = "Page size must be between 1 and 100." });
            }

            _logger.LogInformation("Fetching bookings - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            
            var result = await _bookingService.GetAllBookingsAsync(page, pageSize);
            
            _logger.LogInformation("Successfully retrieved {Count} bookings from {Total} total", 
                result.Items.Count, result.Total);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings with pagination");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while retrieving bookings." });
        }
    }

    /// <summary>
    /// Gets a specific booking by ID.
    /// </summary>
    /// <param name="id">The booking ID to retrieve</param>
    /// <returns>200 OK with booking details or 404 Not Found</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponseDto>> GetById(int id)
    {
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid booking ID requested: {Id}", id);
                return BadRequest(new { message = "Booking ID must be greater than 0." });
            }

            _logger.LogInformation("Fetching booking with ID: {BookingId}", id);
            
            var booking = await _bookingService.GetBookingByIdAsync(id);
            
            _logger.LogInformation("Successfully retrieved booking {BookingId}", id);
            
            return Ok(booking);
        }
        catch (BookingNotFoundException ex)
        {
            _logger.LogWarning(ex, "Booking not found with ID: {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking with ID: {BookingId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while retrieving the booking." });
        }
    }

    /// <summary>
    /// Creates a new booking.
    /// Validates flight availability, passenger details, and creates the booking.
    /// </summary>
    /// <param name="dto">Booking creation data</param>
    /// <returns>201 Created with new booking details and Location header</returns>
    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> Create([FromBody] BookingCreateDto dto)
    {
        try
        {
            // Validate ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid booking creation request: {@ModelState}", ModelState);
                return BadRequest(new { message = "Invalid booking data.", errors = ModelState });
            }

            // Validate DTO content
            if (dto.FlightId <= 0)
            {
                _logger.LogWarning("Invalid flight ID in booking request: {FlightId}", dto.FlightId);
                return BadRequest(new { message = "Flight ID must be greater than 0." });
            }

            if (dto.PassengerCount <= 0)
            {
                _logger.LogWarning("Invalid passenger count in booking request: {PassengerCount}", dto.PassengerCount);
                return BadRequest(new { message = "Passenger count must be at least 1." });
            }

            if (dto.Passengers == null || dto.Passengers.Count == 0)
            {
                _logger.LogWarning("No passengers provided in booking request");
                return BadRequest(new { message = "At least one passenger is required." });
            }

            if (dto.Passengers.Count != dto.PassengerCount)
            {
                _logger.LogWarning("Passenger count mismatch: specified {Count}, provided {Provided}", 
                    dto.PassengerCount, dto.Passengers.Count);
                return BadRequest(new { message = "Passenger count must match the number of passengers provided." });
            }

            // Get userId from claims (in real scenario, extract from JWT token)
            // For now, using a placeholder - should be extracted from authenticated user
            var userId = 1; // TODO: Extract from User.FindFirst(ClaimTypes.NameIdentifier)

            _logger.LogInformation("Creating booking for user {UserId} on flight {FlightId} with {PassengerCount} passengers",
                userId, dto.FlightId, dto.PassengerCount);

            var booking = await _bookingService.CreateBookingAsync(dto, userId);

            _logger.LogInformation("Successfully created booking {BookingId} with reference {BookingReference}",
                booking.Id, booking.BookingReference);

            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
        }
        catch (FlightNotFoundException ex)
        {
            _logger.LogWarning(ex, "Flight not found for booking creation");
            return NotFound(new { message = ex.Message });
        }
        catch (InsufficientSeatsException ex)
        {
            _logger.LogWarning(ex, "Insufficient seats for booking creation");
            return BadRequest(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in booking creation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the booking." });
        }
    }

    /// <summary>
    /// Cancels an existing booking.
    /// Releases reserved seats and processes refund.
    /// </summary>
    /// <param name="id">The booking ID to cancel</param>
    /// <returns>200 OK with updated booking details</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<BookingResponseDto>> Cancel(int id)
    {
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid booking ID for cancellation: {Id}", id);
                return BadRequest(new { message = "Booking ID must be greater than 0." });
            }

            // Get userId from claims (placeholder - should extract from JWT)
            var userId = 1; // TODO: Extract from User.FindFirst(ClaimTypes.NameIdentifier)

            _logger.LogInformation("Cancelling booking {BookingId} for user {UserId}", id, userId);

            var booking = await _bookingService.CancelBookingAsync(id, userId);

            _logger.LogInformation("Successfully cancelled booking {BookingId}", id);

            return Ok(booking);
        }
        catch (BookingNotFoundException ex)
        {
            _logger.LogWarning(ex, "Booking not found for cancellation: {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (BookingAlreadyCancelledException ex)
        {
            _logger.LogWarning(ex, "Booking already cancelled: {BookingId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidBookingStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid booking status for cancellation: {BookingId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized cancellation attempt for booking: {BookingId}", id);
            return Unauthorized(new { message = "You are not authorized to cancel this booking." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while cancelling the booking." });
        }
    }

    /// <summary>
    /// Checks in a passenger for their booking.
    /// Updates booking status from Confirmed to CheckedIn.
    /// </summary>
    /// <param name="id">The booking ID to check in</param>
    /// <returns>200 OK with updated booking details</returns>
    [HttpPut("{id}/check-in")]
    public async Task<ActionResult<BookingResponseDto>> CheckIn(int id)
    {
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid booking ID for check-in: {Id}", id);
                return BadRequest(new { message = "Booking ID must be greater than 0." });
            }

            // Get userId from claims (placeholder - should extract from JWT)
            var userId = 1; // TODO: Extract from User.FindFirst(ClaimTypes.NameIdentifier)

            _logger.LogInformation("Checking in booking {BookingId} for user {UserId}", id, userId);

            var booking = await _bookingService.CheckInBookingAsync(id, userId);

            _logger.LogInformation("Successfully checked in booking {BookingId}", id);

            return Ok(booking);
        }
        catch (BookingNotFoundException ex)
        {
            _logger.LogWarning(ex, "Booking not found for check-in: {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidBookingStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid booking status for check-in: {BookingId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized check-in attempt for booking: {BookingId}", id);
            return Unauthorized(new { message = "You are not authorized to check in this booking." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in booking {BookingId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while checking in the booking." });
        }
    }
}
