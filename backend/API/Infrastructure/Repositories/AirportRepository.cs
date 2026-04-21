namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AirportRepository : IAirportRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<AirportRepository> _logger;

    public AirportRepository(FlightBookingDbContext context, ILogger<AirportRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Airport?> GetByCodeAsync(string code)
    {
        try
        {
            return await _context.Airports.FirstOrDefaultAsync(a => a.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting airport by code: {Code}", code);
            throw;
        }
    }

    public async Task<IEnumerable<Airport>> GetAllActiveAsync()
    {
        try
        {
            return await _context.Airports.Where(a => a.IsActive).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active airports");
            throw;
        }
    }

    public async Task<Airport> CreateAsync(Airport airport)
    {
        try
        {
            await _context.Airports.AddAsync(airport);
            await _context.SaveChangesAsync();
            return airport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating airport");
            throw;
        }
    }

    public async Task UpdateAsync(Airport airport)
    {
        try
        {
            _context.Airports.Update(airport);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating airport");
            throw;
        }
    }

    public async Task<Airport?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Airports.FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting airport by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Airport>> GetAllAsync()
    {
        try
        {
            return await _context.Airports.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all airports");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var airport = await GetByIdAsync(id);
            if (airport != null)
            {
                _context.Airports.Remove(airport);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting airport");
            throw;
        }
    }
}
