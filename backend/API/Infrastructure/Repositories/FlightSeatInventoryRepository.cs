namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Application.Exceptions;
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

    public async Task<bool> TryUpdateWithConcurrencyCheckAsync(int id, Func<FlightSeatInventory, bool> updateAction)
    {
        try
        {
            var inventory = await _context.FlightSeatInventories
                .FirstOrDefaultAsync(fsi => fsi.Id == id);
            
            if (inventory == null)
            {
                return false;
            }

            var originalVersion = inventory.Version;
            var updated = updateAction(inventory);

            if (!updated)
            {
                return false;
            }

            inventory.Version = originalVersion + 1;
            _context.FlightSeatInventories.Update(inventory);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict when updating seat inventory {Id}", id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating seat inventory with concurrency check: {Id}", id);
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
            if (inventory == null)
            {
                throw new NotFoundException($"Seat inventory with id {id} not found");
            }

            if (inventory.Version != version)
            {
                throw new ConcurrencyException("Seat inventory has been modified. Please refresh and try again.");
            }

            inventory.HoldSeats(count);
            inventory.Version = version + 1;
            
            _context.FlightSeatInventories.Update(inventory);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error holding seats for inventory {Id}", id);
            throw;
        }
    }

    public async Task UpdateAsync(FlightSeatInventory inventory)
    {
        try
        {
            // Optimistic concurrency check using Version field
            var existing = await _context.FlightSeatInventories
                .FirstOrDefaultAsync(fsi => fsi.Id == inventory.Id);
            
            if (existing == null)
            {
                throw new NotFoundException($"Seat inventory with id {inventory.Id} not found");
            }

            // Verify version hasn't changed since we read it
            if (existing.Version != inventory.Version)
            {
                throw new ConcurrencyException(
                    $"Seat inventory has been modified. Current version: {existing.Version}, attempted version: {inventory.Version}");
            }

            // Update the entity and increment version
            inventory.Version = existing.Version + 1;
            _context.FlightSeatInventories.Update(inventory);
            
            var affected = await _context.SaveChangesAsync();
            
            if (affected == 0)
            {
                throw new ConcurrencyException("Failed to update seat inventory - concurrent modification detected");
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict when updating seat inventory {Id}", inventory.Id);
            throw new ConcurrencyException("Seat inventory has been modified by another process. Please refresh and try again.");
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
