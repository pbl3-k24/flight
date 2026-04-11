using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Repositories;

public class NotificationJobRepository(AppDbContext db) : INotificationJobRepository
{
    public async Task<IEnumerable<NotificationJob>> GetPendingAsync(int batchSize) =>
        await db.NotificationJobs.Where(j => j.Status == "pending")
                                 .OrderBy(j => j.CreatedAt).Take(batchSize).ToListAsync();

    public async Task AddAsync(NotificationJob job) => await db.NotificationJobs.AddAsync(job);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class EmailTemplateRepository(AppDbContext db) : IEmailTemplateRepository
{
    public Task<EmailTemplate?> GetByKeyAsync(string templateKey) =>
        db.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateKey == templateKey);
}

public class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task<IEnumerable<AuditLog>> GetFilteredAsync(
        Guid? userId, string? action, string? entityType,
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = db.AuditLogs.AsQueryable();
        if (userId.HasValue) query = query.Where(l => l.UserId == userId);
        if (!string.IsNullOrEmpty(action)) query = query.Where(l => l.Action == action);
        if (!string.IsNullOrEmpty(entityType)) query = query.Where(l => l.EntityType == entityType);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value);
        return await query.OrderByDescending(l => l.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task AddAsync(AuditLog log) => await db.AuditLogs.AddAsync(log);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
