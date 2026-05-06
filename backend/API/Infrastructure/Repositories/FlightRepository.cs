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
                .Include(f => f.ActualAircraft)
                .FirstOrDefaultAsync(f => f.FlightDefinition.FlightNumber == flightNumber);
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
                .Include(f => f.ActualAircraft)
                .Where(f => f.FlightDefinition.Route.DepartureAirportId == departureId 
                    && f.FlightDefinition.Route.ArrivalAirportId == arrivalId 
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
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);
            
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
                .Include(f => f.ActualAircraft)
                .Where(f => f.DepartureTime >= start && f.DepartureTime < end)
                .ToListAsync();
            
            _logger.LogInformation("Found {Count} flights in date range", allFlights.Count);
            
            // Filter by route
            var filtered = allFlights
                .Where(f => f.FlightDefinition != null 
                    && f.FlightDefinition.Route != null
                    && f.FlightDefinition.Route.DepartureAirportId == departureAirportId 
                    && f.FlightDefinition.Route.ArrivalAirportId == arrivalAirportId)
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
                f.FlightDefinition.RouteId == routeId
                && (f.ActualAircraftId == aircraftId || f.FlightDefinition.DefaultAircraftId == aircraftId)
                && f.DepartureTime == departureTime
                && f.FlightDefinition.FlightNumber == flightNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking flight existence for {FlightNumber}", flightNumber);
            throw;
        }
    }

    public async Task<IEnumerable<Flight>> GetFlightsByRouteAndDateAsync(int routeId, DateTime departureDate)
    {
        try
        {
            var start = departureDate.Date;
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
                .Include(f => f.ActualAircraft)
                .Where(f => f.FlightDefinition.RouteId == routeId && f.DepartureTime >= start && f.DepartureTime < end)
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
