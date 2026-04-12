namespace API.Application.Interfaces;

using API.Domain.Entities;

/// <summary>
/// Repository interface for passenger data access operations.
/// Defines contracts for database operations on passengers.
/// </summary>
public interface IPassengerRepository
{
    /// <summary>
    /// Gets all passengers for a specific booking.
    /// </summary>
    /// <param name="bookingId">The booking ID to filter by.</param>
    /// <returns>Collection of passengers in the booking.</returns>
    Task<IEnumerable<Passenger>> GetByBookingIdAsync(int bookingId);

    /// <summary>
    /// Adds a new passenger to the database.
    /// </summary>
    /// <param name="passenger">The passenger entity to add.</param>
    /// <returns>The added passenger with assigned ID.</returns>
    Task<Passenger> AddAsync(Passenger passenger);

    /// <summary>
    /// Adds multiple passengers to the database.
    /// </summary>
    /// <param name="passengers">The passenger entities to add.</param>
    /// <returns>Collection of added passengers with assigned IDs.</returns>
    Task<IEnumerable<Passenger>> AddRangeAsync(IEnumerable<Passenger> passengers);

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <returns>Number of entities affected.</returns>
    Task<int> SaveChangesAsync();
}
