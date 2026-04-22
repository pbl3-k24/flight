namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class SeatClassRepository : ISeatClassRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<SeatClassRepository> _logger;

    public SeatClassRepository(FlightBookingDbContext context, ILogger<SeatClassRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<SeatClass?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.SeatClasses.FirstOrDefaultAsync(sc => sc.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat class by id: {Id}", id);
            throw;
        }
    }

    public async Task<SeatClass?> GetByCodeAsync(string code)
    {
        try
        {
            return await _context.SeatClasses.FirstOrDefaultAsync(sc => sc.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat class by code: {Code}", code);
            throw;
        }
    }

    public async Task<IEnumerable<SeatClass>> GetAllAsync()
    {
        try
        {
            return await _context.SeatClasses.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all seat classes");
            throw;
        }
    }

    public async Task<SeatClass> CreateAsync(SeatClass seatClass)
    {
        try
        {
            await _context.SeatClasses.AddAsync(seatClass);
            await _context.SaveChangesAsync();
            return seatClass;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating seat class");
            throw;
        }
    }

    public async Task UpdateAsync(SeatClass seatClass)
    {
        try
        {
            _context.SeatClasses.Update(seatClass);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating seat class");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var seatClass = await GetByIdAsync(id);
            if (seatClass != null)
            {
                _context.SeatClasses.Remove(seatClass);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting seat class");
            throw;
        }
    }
}
