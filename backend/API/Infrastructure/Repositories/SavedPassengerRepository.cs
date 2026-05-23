namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class SavedPassengerRepository : ISavedPassengerRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<SavedPassengerRepository> _logger;

    public SavedPassengerRepository(FlightBookingDbContext context, ILogger<SavedPassengerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SavedPassenger>> GetByUserIdAsync(int userId)
    {
        try
        {
            return await _context.SavedPassengers
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.IsDefaultOwner)
                .ThenBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved passengers for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SavedPassenger?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.SavedPassengers
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved passenger {PassengerId}", id);
            throw;
        }
    }

    public async Task<SavedPassenger?> GetDefaultByUserIdAsync(int userId)
    {
        try
        {
            return await _context.SavedPassengers
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefaultOwner && !x.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default saved passenger for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SavedPassenger> CreateAsync(SavedPassenger passenger)
    {
        try
        {
            await _context.SavedPassengers.AddAsync(passenger);
            await _context.SaveChangesAsync();
            return passenger;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating saved passenger for user {UserId}", passenger.UserId);
            throw;
        }
    }

    public async Task UpdateAsync(SavedPassenger passenger)
    {
        try
        {
            _context.SavedPassengers.Update(passenger);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating saved passenger {PassengerId}", passenger.Id);
            throw;
        }
    }
}

