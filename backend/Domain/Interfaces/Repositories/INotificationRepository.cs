using FlightBooking.Domain.Entities;

namespace FlightBooking.Domain.Interfaces.Repositories;

public interface INotificationJobRepository
{
    Task<IEnumerable<NotificationJob>> GetPendingAsync(int batchSize);
    Task AddAsync(NotificationJob job);
    Task SaveChangesAsync();
}

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByKeyAsync(string templateKey);
}

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetFilteredAsync(
        Guid? userId, string? action, string? entityType,
        DateTime? from, DateTime? to, int page, int pageSize);
    Task AddAsync(AuditLog log);
    Task SaveChangesAsync();
}
