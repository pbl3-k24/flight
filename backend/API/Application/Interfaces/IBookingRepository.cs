namespace API.Application.Interfaces;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Repository interface for booking data access operations.
/// Extends the generic IRepository with booking-specific query methods.
/// Defines contracts for database operations on bookings.
/// </summary>
public interface IBookingRepository : IRepository<Booking>
{
    /// <summary>
    /// Gets all bookings for a specific user with pagination.
    /// </summary>
    /// <param name="userId">The user ID to filter by.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Collection of bookings for the user.</returns>
    Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, int page, int pageSize);

    /// <summary>
    /// Gets the total count of bookings for a user.
    /// </summary>
    /// <param name="userId">The user ID to count bookings for.</param>
    /// <returns>Total count of bookings.</returns>
    Task<int> GetCountByUserIdAsync(int userId);

    /// <summary>
    /// Finds a booking by its booking reference code.
    /// </summary>
    /// <param name="bookingReference">The booking reference code.</param>
    /// <returns>Booking entity if found, null otherwise.</returns>
    Task<Booking?> GetByReferenceAsync(string bookingReference);

    /// <summary>
    /// Gets all bookings for a specific flight.
    /// </summary>
    /// <param name="flightId">The flight ID to filter by.</param>
    /// <returns>Collection of bookings for the flight.</returns>
    Task<IEnumerable<Booking>> GetByFlightIdAsync(int flightId);

    /// <summary>
    /// Gets a booking with all related data loaded (eager loading).
    /// </summary>
    /// <param name="id">The booking ID to retrieve.</param>
    /// <returns>Booking entity with passengers and payment if found, null otherwise.</returns>
    Task<Booking?> GetWithDetailsAsync(int id);

    /// <summary>
    /// Checks if a booking reference is unique.
    /// </summary>
    /// <param name="bookingReference">The booking reference to check.</param>
    /// <returns>True if reference is unique, false if already exists.</returns>
    Task<bool> IsReferenceUniqueAsync(string bookingReference);

    /// <summary>
    /// Gets bookings by status.
    /// </summary>
    /// <param name="status">Booking status to filter by.</param>
    /// <returns>Collection of bookings with the specified status.</returns>
    Task<IEnumerable<Booking>> GetByStatusAsync(Domain.Enums.BookingStatus status);

    /// <summary>
    /// Gets bookings created within a specific date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <returns>Collection of bookings created within the date range.</returns>
    Task<IEnumerable<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets paged bookings with related details.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paged bookings.</returns>
    Task<IEnumerable<Booking>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// Gets total booking count.
    /// </summary>
    /// <returns>Total booking count.</returns>
    Task<int> GetCountAsync();

    /// <summary>
    /// Begins a database transaction for atomic operations.
    /// </summary>
    /// <returns>Transaction handle.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync();
}
