namespace API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Domain.Enums;
using API.Infrastructure.Data;

/// <summary>
/// Repository for flight data access operations.
/// Implements IFlightRepository using Entity Framework Core.
/// Provides flight-specific query operations with optimized LINQ patterns.
/// </summary>
public class FlightRepository : BaseRepository<Flight>, IFlightRepository
{
    /// <summary>
    /// Logger instance for debugging and error tracking.
    /// </summary>
    private readonly ILogger<FlightRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the FlightRepository class.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="logger">Logger for repository operations.</param>
    public FlightRepository(FlightBookingDbContext context, ILogger<FlightRepository> logger) : base(context)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a flight by ID with all related bookings and passengers loaded (eager loading).
    /// Algorithm from 04_INFRASTRUCTURE_LAYER_GUIDE.md:
    /// 1. Query DbSet
    /// 2. Include(f => f.Bookings)
    /// 3. ThenInclude(b => b.Passengers)
    /// 4. Where(f => f.Id == id)
    /// 5. FirstOrDefaultAsync()
    /// 6. Return result
    /// </summary>
    /// <param name="id">The flight ID to retrieve.</param>
    /// <returns>Flight entity with bookings and passengers if found, null otherwise.</returns>
    public async Task<Flight?> GetFlightWithBookingsAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching flight {FlightId} with bookings", id);

            var flight = await DbSet
                .Include(f => f.Bookings)
                    .ThenInclude(b => b.Passengers)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync();

            if (flight == null)
            {
                _logger.LogWarning("Flight {FlightId} not found", id);
                return null;
            }

            _logger.LogInformation("Successfully retrieved flight {FlightId} with {BookingCount} bookings",
                id, flight.Bookings?.Count ?? 0);

            return flight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight {FlightId} with bookings", id);
            throw;
        }
    }

    /// <summary>
    /// Gets available flights matching search criteria.
    /// Algorithm from 04_INFRASTRUCTURE_LAYER_GUIDE.md:
    /// 1. Query DbSet with AsNoTracking()
    /// 2. Filter: WHERE departure_airport_id = @id
    /// 3. Filter: AND arrival_airport_id = @id
    /// 4. Filter: AND DATE(departure_time) = @date
    /// 5. Filter: AND status = Active
    /// 6. Order by: departure_time ascending
    /// 7. ToListAsync()
    /// 8. Return results
    /// </summary>
    /// <param name="departureAirportId">Departure airport ID to filter by.</param>
    /// <param name="arrivalAirportId">Arrival airport ID to filter by.</param>
    /// <param name="departureDate">Departure date to filter by (date only).</param>
    /// <returns>Collection of matching flights sorted by departure time.</returns>
    public async Task<IEnumerable<Flight>> GetAvailableFlightsAsync(
        int departureAirportId,
        int arrivalAirportId,
        DateTime departureDate)
    {
        try
        {
            _logger.LogInformation(
                "Fetching available flights from {DepartureId} to {ArrivalId} on {DepartureDate}",
                departureAirportId, arrivalAirportId, departureDate.Date);

            var flights = await DbSet
                .AsNoTracking()
                .Where(f => f.DepartureAirportId == departureAirportId)
                .Where(f => f.ArrivalAirportId == arrivalAirportId)
                .Where(f => f.DepartureTime.Date == departureDate.Date)
                .Where(f => f.Status == FlightStatus.Active)
                .OrderBy(f => f.DepartureTime)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .ToListAsync();

            _logger.LogInformation("Found {Count} available flights", flights.Count);

            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available flights");
            throw;
        }
    }

    /// <summary>
    /// Gets paged results of all flights with pagination support.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Collection of flights for the requested page.</returns>
    public async Task<IEnumerable<Flight>> GetPagedAsync(int page, int pageSize)
    {
        try
        {
            _logger.LogInformation("Fetching paged flights - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            var skip = (page - 1) * pageSize;

            var flights = await DbSet
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} flights from page {Page}", flights.Count, page);

            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paged flights");
            throw;
        }
    }

    /// <summary>
    /// Gets the total count of all flights in the database.
    /// </summary>
    /// <returns>Total count of flights.</returns>
    public async Task<int> GetCountAsync()
    {
        try
        {
            var count = await DbSet.AsNoTracking().CountAsync();
            _logger.LogInformation("Total flight count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight count");
            throw;
        }
    }

    /// <summary>
    /// Searches for flights matching the specified criteria with optional seat filtering.
    /// </summary>
    /// <param name="departureAirportId">Departure airport ID to filter by.</param>
    /// <param name="arrivalAirportId">Arrival airport ID to filter by.</param>
    /// <param name="departureDate">Departure date to filter by (date only).</param>
    /// <param name="minAvailableSeats">Minimum available seats required.</param>
    /// <returns>Collection of matching flights sorted by departure time.</returns>
    public async Task<IEnumerable<Flight>> SearchAsync(
        int departureAirportId,
        int arrivalAirportId,
        DateTime departureDate,
        int minAvailableSeats = 0)
    {
        try
        {
            _logger.LogInformation(
                "Searching flights from {DepartureId} to {ArrivalId} on {DepartureDate} with min {MinSeats} seats",
                departureAirportId, arrivalAirportId, departureDate.Date, minAvailableSeats);

            var flights = await DbSet
                .AsNoTracking()
                .Where(f => f.DepartureAirportId == departureAirportId)
                .Where(f => f.ArrivalAirportId == arrivalAirportId)
                .Where(f => f.DepartureTime.Date == departureDate.Date)
                .Where(f => f.Status == FlightStatus.Active)
                .Where(f => f.AvailableSeats >= minAvailableSeats)
                .OrderBy(f => f.DepartureTime)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .ToListAsync();

            _logger.LogInformation("Search returned {Count} flights", flights.Count);

            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching flights");
            throw;
        }
    }

    /// <summary>
    /// Checks if a flight exists by ID.
    /// </summary>
    /// <param name="id">The flight ID to check.</param>
    /// <returns>True if flight exists, false otherwise.</returns>
    public async Task<bool> ExistsAsync(int id)
    {
        try
        {
            return await DbSet.AsNoTracking().AnyAsync(f => f.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking flight existence");
            throw;
        }
    }

    /// <summary>
    /// Gets flights by status.
    /// </summary>
    /// <param name="status">Flight status to filter by.</param>
    /// <returns>Collection of flights with the specified status.</returns>
    public async Task<IEnumerable<Flight>> GetByStatusAsync(FlightStatus status)
    {
        try
        {
            _logger.LogInformation("Fetching flights with status: {Status}", status);

            var flights = await DbSet
                .AsNoTracking()
                .Where(f => f.Status == status)
                .OrderByDescending(f => f.CreatedAt)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .ToListAsync();

            _logger.LogInformation("Found {Count} flights with status {Status}", flights.Count, status);

            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching flights by status");
            throw;
        }
    }

    /// <summary>
    /// Gets flights departing within a specific date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <returns>Collection of flights departing within the date range.</returns>
    public async Task<IEnumerable<Flight>> GetFlightsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation(
                "Fetching flights between {StartDate} and {EndDate}",
                startDate.Date, endDate.Date);

            var flights = await DbSet
                .AsNoTracking()
                .Where(f => f.DepartureTime.Date >= startDate.Date && f.DepartureTime.Date <= endDate.Date)
                .OrderBy(f => f.DepartureTime)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .ToListAsync();

            _logger.LogInformation("Found {Count} flights in date range", flights.Count);

            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching flights by date range");
            throw;
        }
    }
}
