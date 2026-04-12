namespace API.Application.Interfaces;

using API.Domain.Entities;

/// <summary>
/// Repository interface for flight data access operations.
/// Extends the generic IRepository with flight-specific query methods.
/// Defines contracts for database operations on flights.
/// </summary>
public interface IFlightRepository : IRepository<Flight>
{
    /// <summary>
    /// Gets a flight by ID with all related data loaded (eager loading).
    /// </summary>
    /// <param name="id">The flight ID to retrieve.</param>
    /// <returns>Flight entity with bookings if found, null otherwise.</returns>
    Task<Flight?> GetFlightWithBookingsAsync(int id);

    /// <summary>
    /// Gets available flights matching search criteria.
    /// </summary>
    /// <param name="departureAirportId">Departure airport ID to filter by.</param>
    /// <param name="arrivalAirportId">Arrival airport ID to filter by.</param>
    /// <param name="departureDate">Departure date to filter by (date only).</param>
    /// <returns>Collection of matching flights with available seats, sorted by departure time.</returns>
    Task<IEnumerable<Flight>> GetAvailableFlightsAsync(int departureAirportId, int arrivalAirportId, DateTime departureDate);

    /// <summary>
    /// Gets paged results of all flights.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Collection of flights for the requested page.</returns>
    Task<IEnumerable<Flight>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// Gets the total count of all flights in the database.
    /// </summary>
    /// <returns>Total count of flights.</returns>
    Task<int> GetCountAsync();

    /// <summary>
    /// Searches for flights matching the specified criteria with optional filtering.
    /// </summary>
    /// <param name="departureAirportId">Departure airport ID to filter by.</param>
    /// <param name="arrivalAirportId">Arrival airport ID to filter by.</param>
    /// <param name="departureDate">Departure date to filter by (date only).</param>
    /// <param name="minAvailableSeats">Minimum available seats required.</param>
    /// <returns>Collection of matching flights sorted by departure time.</returns>
    Task<IEnumerable<Flight>> SearchAsync(int departureAirportId, int arrivalAirportId, DateTime departureDate, int minAvailableSeats = 0);

    /// <summary>
    /// Checks if a flight exists by ID.
    /// </summary>
    /// <param name="id">The flight ID to check.</param>
    /// <returns>True if flight exists, false otherwise.</returns>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Gets flights by status.
    /// </summary>
    /// <param name="status">Flight status to filter by.</param>
    /// <returns>Collection of flights with the specified status.</returns>
    Task<IEnumerable<Flight>> GetByStatusAsync(Domain.Enums.FlightStatus status);

    /// <summary>
    /// Gets flights departing within a specific date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <returns>Collection of flights departing within the date range.</returns>
    Task<IEnumerable<Flight>> GetFlightsByDateRangeAsync(DateTime startDate, DateTime endDate);
}
