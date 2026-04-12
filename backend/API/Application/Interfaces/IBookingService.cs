namespace API.Application.Interfaces;

using API.Application.DTOs;

/// <summary>
/// Service interface for booking-related operations.
/// Defines contracts for booking business logic orchestration.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Gets all bookings with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated list of bookings.</returns>
    Task<PaginatedBookingsResponseDto> GetAllBookingsAsync(int page, int pageSize);

    /// <summary>
    /// Gets a specific booking by ID.
    /// </summary>
    /// <param name="bookingId">The booking ID to retrieve.</param>
    /// <returns>Booking details if found.</returns>
    /// <exception cref="BookingNotFoundException">Thrown when booking is not found.</exception>
    Task<BookingResponseDto> GetBookingByIdAsync(int bookingId);

    /// <summary>
    /// Creates a new booking.
    /// Validates flight availability, user existence, and passenger details.
    /// </summary>
    /// <param name="dto">Booking creation data.</param>
    /// <param name="userId">ID of the user creating the booking.</param>
    /// <returns>Newly created booking details.</returns>
    /// <exception cref="FlightNotFoundException">Thrown when flight is not found.</exception>
    /// <exception cref="InsufficientSeatsException">Thrown when flight has insufficient seats.</exception>
    /// <exception cref="ValidationException">Thrown when booking data is invalid.</exception>
    Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto dto, int userId);

    /// <summary>
    /// Cancels an existing booking.
    /// Releases seats and initiates refund process.
    /// </summary>
    /// <param name="bookingId">The booking ID to cancel.</param>
    /// <param name="userId">ID of the user cancelling the booking (for authorization).</param>
    /// <returns>The cancelled booking details.</returns>
    /// <exception cref="BookingNotFoundException">Thrown when booking is not found.</exception>
    /// <exception cref="BookingAlreadyCancelledException">Thrown when booking is already cancelled.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the booking.</exception>
    Task<BookingResponseDto> CancelBookingAsync(int bookingId, int userId);

    /// <summary>
    /// Checks in a passenger for their booking.
    /// Updates booking status to CheckedIn.
    /// </summary>
    /// <param name="bookingId">The booking ID to check in.</param>
    /// <param name="userId">ID of the user checking in (for authorization).</param>
    /// <returns>The updated booking details.</returns>
    /// <exception cref="BookingNotFoundException">Thrown when booking is not found.</exception>
    /// <exception cref="InvalidBookingStatusException">Thrown when booking cannot be checked in.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the booking.</exception>
    Task<BookingResponseDto> CheckInBookingAsync(int bookingId, int userId);
}
