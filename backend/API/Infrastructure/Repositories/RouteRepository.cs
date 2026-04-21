namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RouteRepository : IRouteRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<RouteRepository> _logger;

    public RouteRepository(FlightBookingDbContext context, ILogger<RouteRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Route?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Routes
                .Include(r => r.DepartureAirport)
                .Include(r => r.ArrivalAirport)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Route>> GetByAirportsAsync(int departureAirportId, int arrivalAirportId)
    {
        try
        {
            return await _context.Routes
                .Include(r => r.DepartureAirport)
                .Include(r => r.ArrivalAirport)
                .Where(r => r.DepartureAirportId == departureAirportId && r.ArrivalAirportId == arrivalAirportId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting routes by airports");
            throw;
        }
    }

    public async Task<IEnumerable<Route>> GetAllAsync()
    {
        try
        {
            return await _context.Routes
                .Include(r => r.DepartureAirport)
                .Include(r => r.ArrivalAirport)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all routes");
            throw;
        }
    }

    public async Task<Route> CreateAsync(Route route)
    {
        try
        {
            await _context.Routes.AddAsync(route);
            await _context.SaveChangesAsync();
            return route;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating route");
            throw;
        }
    }

    public async Task UpdateAsync(Route route)
    {
        try
        {
            _context.Routes.Update(route);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating route");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var route = await GetByIdAsync(id);
            if (route != null)
            {
                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting route");
            throw;
        }
    }
}
