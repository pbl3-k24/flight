using FlightBooking.Application.DTOs.Admin;

namespace FlightBooking.Application.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, string? entityId, object? before, object? after, Guid? userId = null, string? ipAddress = null);
    Task<IEnumerable<AuditLogDto>> GetLogsAsync(AuditLogFilter filter);
}
