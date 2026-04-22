namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class FlightSeatInventoryRepository : IFlightSeatInventoryRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<FlightSeatInventoryRepository> _logger;

    public FlightSeatInventoryRepository(FlightBookingDbContext context, ILogger<FlightSeatInventoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<FlightSeatInventory?> GetAsync(int flightId, int seatClassId)
    {
        try
        {
            return await _context.FlightSeatInventories
                .FirstOrDefaultAsync(fsi => fsi.FlightId == flightId && fsi.SeatClassId == seatClassId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat inventory for flight {FlightId} and seat class {SeatClassId}", flightId, seatClassId);
            throw;
        }
    }

    public async Task<IEnumerable<FlightSeatInventory>> GetAllForFlightAsync(int flightId)
    {
        try
        {
            return await _context.FlightSeatInventories
                .Include(fsi => fsi.SeatClass)
                .Where(fsi => fsi.FlightId == flightId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat inventories for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<List<FlightSeatInventory>> GetByFlightIdAsync(int flightId)
    {
        try
        {
            return await _context.FlightSeatInventories
                .Include(fsi => fsi.SeatClass)
                .Where(fsi => fsi.FlightId == flightId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat inventories by flight id: {FlightId}", flightId);
            throw;
        }
    }

    public async Task<FlightSeatInventory?> GetByFlightAndSeatClassAsync(int flightId, int seatClassId)
    {
        try
        {
            return await _context.FlightSeatInventories
                .Include(fsi => fsi.SeatClass)
                .FirstOrDefaultAsync(fsi => fsi.FlightId == flightId && fsi.SeatClassId == seatClassId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat inventory by flight and seat class");
            throw;
        }
    }

    public async Task<List<FlightSeatInventory>> GetActiveInventoriesAsync()
    {
        try
        {
            return await _context.FlightSeatInventories
                .Include(fsi => fsi.Flight)
                .Include(fsi => fsi.SeatClass)
                .Where(fsi => fsi.AvailableSeats > 0)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active inventories");
            throw;
        }
    }

    public async Task ReserveSeatsAsync(int id, int count, int version)
    {
        try
        {
            var inventory = await _context.FlightSeatInventories.FirstOrDefaultAsync(fsi => fsi.Id == id);
            if (inventory != null && inventory.Version == version)
            {
                inventory.HoldSeats(count);
                _context.FlightSeatInventories.Update(inventory);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error holding seats");
            throw;
        }
    }

    public async Task UpdateAsync(FlightSeatInventory inventory)
    {
        try
        {
            _context.FlightSeatInventories.Update(inventory);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating seat inventory");
            throw;
        }
    }

    public async Task<FlightSeatInventory?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.FlightSeatInventories
                .Include(fsi => fsi.Flight)
                .Include(fsi => fsi.SeatClass)
                .FirstOrDefaultAsync(fsi => fsi.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat inventory by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<FlightSeatInventory>> GetAllAsync()
    {
        try
        {
            return await _context.FlightSeatInventories
                .Include(fsi => fsi.Flight)
                .Include(fsi => fsi.SeatClass)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all seat inventories");
            throw;
        }
    }

    public async Task CreateAsync(FlightSeatInventory inventory)
    {
        try
        {
            await _context.FlightSeatInventories.AddAsync(inventory);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating seat inventory");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var inventory = await GetByIdAsync(id);
            if (inventory != null)
            {
                _context.FlightSeatInventories.Remove(inventory);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting seat inventory");
            throw;
        }
    }
}
