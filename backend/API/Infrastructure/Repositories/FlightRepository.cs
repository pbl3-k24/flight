namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class FlightRepository : IFlightRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<FlightRepository> _logger;

    public FlightRepository(FlightBookingDbContext context, ILogger<FlightRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Flight?> GetByFlightNumberAsync(string flightNumber)
    {
        try
        {
            return await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight by number: {FlightNumber}", flightNumber);
            throw;
        }
    }

    public async Task<IEnumerable<Flight>> SearchAsync(int departureId, int arrivalId, DateTime date)
    {
        try
        {
            return await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .Where(f => f.Route.DepartureAirportId == departureId 
                    && f.Route.ArrivalAirportId == arrivalId 
                    && f.DepartureTime.Date == date.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching flights");
            throw;
        }
    }

    public async Task<Flight?> GetWithInventoriesAsync(int id)
    {
        try
        {
            return await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .Include(f => f.SeatInventories)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight with inventories: {Id}", id);
            throw;
        }
    }

    public async Task<Flight> CreateAsync(Flight flight)
    {
        try
        {
            await _context.Flights.AddAsync(flight);
            await _context.SaveChangesAsync();
            return flight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flight");
            throw;
        }
    }

    public async Task UpdateAsync(Flight flight)
    {
        try
        {
            _context.Flights.Update(flight);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight");
            throw;
        }
    }

    public async Task<Flight?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight by id: {Id}", id);
            throw;
        }
    }

    public async Task<Flight?> GetByIdWithDetailsAsync(int id)
    {
        try
        {
            return await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .Include(f => f.SeatInventories)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight with details: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Flight>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Fetching all flights from database");
            var result = await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .ToListAsync();
            _logger.LogDebug("Retrieved {Count} flights", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all flights");
            throw;
        }
    }

    public async Task<IEnumerable<Flight>> GetFlightsByRouteAndDateAsync(int departureAirportId, int arrivalAirportId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var start = startDate;
            var end = endDate;
            if (end <= start)
            {
                end = start.AddDays(1);
            }
            
            _logger.LogInformation("Searching flights: Dep={Dep}, Arr={Arr}, Start={Start}, End={End}", 
                departureAirportId, arrivalAirportId, start, end);
            
            // First, get all flights in date range
            var allFlights = await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .Where(f => f.DepartureTime >= start && f.DepartureTime < end)
                .ToListAsync();
            
            _logger.LogInformation("Found {Count} flights in date range", allFlights.Count);
            
            // Filter by route
            var filtered = allFlights
                .Where(f => f.Route.DepartureAirportId == departureAirportId 
                    && f.Route.ArrivalAirportId == arrivalAirportId)
                .ToList();
            
            _logger.LogInformation("After route filter: {Count} flights", filtered.Count);
            
            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flights by route and date");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string flightNumber, DateTime departureTime, int routeId, int aircraftId)
    {
        try
        {
            return await _context.Flights.AnyAsync(f =>
                f.RouteId == routeId
                && f.AircraftId == aircraftId
                && f.DepartureTime == departureTime
                && f.FlightNumber == flightNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking flight existence for {FlightNumber}", flightNumber);
            throw;
        }
    }

    public async Task<bool> ExistsByDefinitionAndDepartureAsync(int flightDefinitionId, DateTime departureTime)
    {
        try
        {
            return await _context.Flights.AnyAsync(f =>
                !f.IsDeleted &&
                f.FlightDefinitionId == flightDefinitionId &&
                f.DepartureTime == departureTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking flight existence for definition {FlightDefinitionId} at {DepartureTime}",
                flightDefinitionId, departureTime);
            throw;
        }
    }

    public async Task<bool> HasAircraftConflictAsync(int aircraftId, DateTime newDeparture, DateTime newArrival, int turnaroundMinutes)
    {
        try
        {
            var turnaround = TimeSpan.FromMinutes(turnaroundMinutes);
            var newDepartureMinusTurnaround = newDeparture.Subtract(turnaround);
            var newArrivalWithTurnaround = newArrival.Add(turnaround);

            return await _context.Flights.AnyAsync(f =>
                !f.IsDeleted &&
                f.Status != 1 &&
                f.AircraftId == aircraftId &&
                newDepartureMinusTurnaround < f.ArrivalTime &&
                newArrivalWithTurnaround > f.DepartureTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking aircraft conflict for aircraft {AircraftId} in range {Departure} - {Arrival}",
                aircraftId, newDeparture, newArrival);
            throw;
        }
    }

    public async Task AcquireAircraftGenerationLockAsync(int aircraftId)
    {
        try
        {
            // Serialize flight generation per aircraft inside current DB transaction.
            const int flightGenerationLockNamespace = 20260513;
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({flightGenerationLockNamespace}, {aircraftId})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring generation lock for aircraft {AircraftId}", aircraftId);
            throw;
        }
    }

    public async Task<IEnumerable<Flight>> GetFlightsByRouteAndDateAsync(int routeId, DateTime departureDate)
    {
        try
        {
            var start = departureDate;
            var end = start.AddDays(1);
            return await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .Where(f => f.RouteId == routeId && f.DepartureTime >= start && f.DepartureTime < end)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flights by route and date");
            throw;
        }
    }

    public async Task<IEnumerable<Flight>> GetUpcomingFlightsAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(days);
            return await _context.Flights
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.Route)
                        .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FlightDefinition)
                    .ThenInclude(fd => fd.DefaultAircraft)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.ActualAircraft)
                .Where(f => f.DepartureTime >= startDate && f.DepartureTime <= endDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming flights");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var flight = await GetByIdAsync(id);
            if (flight != null)
            {
                _context.Flights.Remove(flight);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flight");
            throw;
        }
    }
}
