namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<NotificationLogRepository> _logger;

    public NotificationLogRepository(FlightBookingDbContext context, ILogger<NotificationLogRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<NotificationLog?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.NotificationLogs.FirstOrDefaultAsync(n => n.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification log by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationLog>> GetByUserIdAsync(int userId)
    {
        try
        {
            return await _context.NotificationLogs
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification logs for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationLog>> GetByStatusAsync(int status)
    {
        try
        {
            return await _context.NotificationLogs
                .Where(n => n.Status == status)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification logs by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationLog>> GetAllAsync()
    {
        try
        {
            return await _context.NotificationLogs
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all notification logs");
            throw;
        }
    }

    public async Task<NotificationLog> CreateAsync(NotificationLog notificationLog)
    {
        try
        {
            await _context.NotificationLogs.AddAsync(notificationLog);
            await _context.SaveChangesAsync();
            return notificationLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification log");
            throw;
        }
    }

    public async Task UpdateAsync(NotificationLog notificationLog)
    {
        try
        {
            _context.NotificationLogs.Update(notificationLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification log");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var notificationLog = await GetByIdAsync(id);
            if (notificationLog != null)
            {
                _context.NotificationLogs.Remove(notificationLog);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification log");
            throw;
        }
    }
}
