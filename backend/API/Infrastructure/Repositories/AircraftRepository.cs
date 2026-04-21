namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AircraftRepository : IAircraftRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<AircraftRepository> _logger;

    public AircraftRepository(FlightBookingDbContext context, ILogger<AircraftRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Aircraft?> GetByRegistrationNumberAsync(string registrationNumber)
    {
        try
        {
            return await _context.Aircraft.FirstOrDefaultAsync(a => a.RegistrationNumber == registrationNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft by registration number: {RegistrationNumber}", registrationNumber);
            throw;
        }
    }

    public async Task<IEnumerable<Aircraft>> GetAllActiveAsync()
    {
        try
        {
            return await _context.Aircraft.Where(a => a.IsActive).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active aircraft");
            throw;
        }
    }

    public async Task<Aircraft> CreateAsync(Aircraft aircraft)
    {
        try
        {
            await _context.Aircraft.AddAsync(aircraft);
            await _context.SaveChangesAsync();
            return aircraft;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating aircraft");
            throw;
        }
    }

    public async Task UpdateAsync(Aircraft aircraft)
    {
        try
        {
            _context.Aircraft.Update(aircraft);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating aircraft");
            throw;
        }
    }

    public async Task<Aircraft?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Aircraft.FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Aircraft>> GetAllAsync()
    {
        try
        {
            return await _context.Aircraft.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all aircraft");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var aircraft = await GetByIdAsync(id);
            if (aircraft != null)
            {
                _context.Aircraft.Remove(aircraft);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting aircraft");
            throw;
        }
    }
}
