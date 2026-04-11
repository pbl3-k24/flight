using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace FlightBooking.Application.Services.Implementations;

public class AuditLogService(IAuditLogRepository auditLogRepository) : IAuditLogService
{
    public async Task LogAsync(string action, string entityType, string? entityId,
        object? before, object? after, Guid? userId = null, string? ipAddress = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = before is null ? null : JsonSerializer.Serialize(before),
            After = after is null ? null : JsonSerializer.Serialize(after),
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
        await auditLogRepository.AddAsync(log);
        await auditLogRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<DTOs.Admin.AuditLogDto>> GetLogsAsync(DTOs.Admin.AuditLogFilter filter)
    {
        var logs = await auditLogRepository.GetFilteredAsync(
            filter.UserId, filter.Action, filter.EntityType, filter.From, filter.To,
            filter.Page, filter.PageSize);

        return logs.Select(l => new DTOs.Admin.AuditLogDto(
            l.Id, l.UserId, l.Action, l.EntityType, l.EntityId,
            l.Before, l.After, l.IpAddress, l.CreatedAt));
    }
}
