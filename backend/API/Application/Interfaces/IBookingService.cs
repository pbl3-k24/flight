namespace API.Application.Interfaces;

using API.Application.Dtos.Booking;
using API.Domain.Entities;

public interface IBookingService
{
    /// <summary>
    /// Creates a new booking with multiple passengers.
    /// </summary>
    /// <param name="userId">User ID making the booking</param>
    /// <param name="dto">Booking details</param>
    /// <returns>Created booking details</returns>
    Task<BookingResponse> CreateBookingAsync(int userId, CreateBookingDto dto);

    /// <summary>
    /// Cancels a booking and initiates refund.
    /// </summary>
    /// <param name="bookingId">Booking ID to cancel</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="reason">Cancellation reason</param>
    /// <returns>Success indicator</returns>
    Task<bool> CancelBookingAsync(int bookingId, int userId, string reason);

    /// <summary>
    /// Updates passenger information in a booking.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="dto">Updated passenger information</param>
    /// <returns>Success indicator</returns>
    Task<bool> UpdateBookingAsync(int bookingId, int userId, UpdateBookingDto dto);

    /// <summary>
    /// Gets full booking details.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <returns>Complete booking information</returns>
    Task<BookingResponse> GetBookingAsync(int bookingId, int userId);

    /// <summary>
    /// Gets all bookings for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated list of bookings</returns>
    Task<List<BookingResponse>> GetUserBookingsAsync(int userId, int page = 1, int pageSize = 10);
}
