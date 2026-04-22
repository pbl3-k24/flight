namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(FlightBookingDbContext context, ILogger<AuditLogRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<AuditLog?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityTypeAsync(string entityType)
    {
        try
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == entityType)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs by entity type: {EntityType}", entityType);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action)
    {
        try
        {
            return await _context.AuditLogs
                .Where(a => a.Action == action)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs by action: {Action}", action);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        try
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all audit logs");
            throw;
        }
    }

    public async Task<AuditLog> CreateAsync(AuditLog auditLog)
    {
        try
        {
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var auditLog = await GetByIdAsync(id);
            if (auditLog != null)
            {
                _context.AuditLogs.Remove(auditLog);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting audit log");
            throw;
        }
    }
}
