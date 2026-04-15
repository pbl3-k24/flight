namespace API.Application.Interfaces;

using API.Application.Dtos.Logging;

public interface IAuditLogService
{
    /// <summary>
    /// Logs an action to audit trail.
    /// </summary>
    Task LogActionAsync(int? userId, string action, string entity, int? entityId, string? oldValues = null, string? newValues = null, string? ipAddress = null);

    /// <summary>
    /// Gets audit logs with filters.
    /// </summary>
    Task<List<AuditLogResponse>> GetAuditLogsAsync(AuditLogFilterDto filter);

    /// <summary>
    /// Gets activity summary.
    /// </summary>
    Task<ActivitySummaryResponse> GetActivitySummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets user activity history.
    /// </summary>
    Task<List<AuditLogResponse>> GetUserActivityAsync(int userId, int days = 30);

    /// <summary>
    /// Gets entity change history.
    /// </summary>
    Task<List<AuditLogResponse>> GetEntityHistoryAsync(string entity, int entityId);
}
