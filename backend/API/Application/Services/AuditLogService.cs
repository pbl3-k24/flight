namespace API.Application.Services;

using API.Application.Dtos.Logging;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task LogActionAsync(int? userId, string action, string entity, int? entityId, string? oldValues = null, string? newValues = null, string? ipAddress = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                ActorId = userId,
                Action = action,
                EntityType = entity,
                EntityId = entityId ?? 0,
                BeforeJson = oldValues,
                AfterJson = newValues,
                CreatedAt = DateTime.UtcNow
            };

            await _auditLogRepository.CreateAsync(auditLog);
            _logger.LogInformation("Audit log created: {Action} on {Entity}", action, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit action");
        }
    }

    public async Task<List<AuditLogResponse>> GetAuditLogsAsync(AuditLogFilterDto filter)
    {
        try
        {
            var logs = await _auditLogRepository.GetAllAsync();
            var responses = new List<AuditLogResponse>();

            var filtered = logs.AsEnumerable();

            if (filter.UserId.HasValue)
            {
                filtered = filtered.Where(l => l.ActorId == filter.UserId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Entity))
            {
                filtered = filtered.Where(l => l.EntityType.Contains(filter.Entity));
            }

            if (!string.IsNullOrEmpty(filter.Action))
            {
                filtered = filtered.Where(l => l.Action.Contains(filter.Action));
            }

            if (filter.FromDate.HasValue)
            {
                filtered = filtered.Where(l => l.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                filtered = filtered.Where(l => l.CreatedAt <= filter.ToDate.Value);
            }

            var paginatedLogs = filtered
                .OrderByDescending(l => l.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize);

            foreach (var log in paginatedLogs)
            {
                var user = log.ActorId.HasValue ? await _userRepository.GetByIdAsync(log.ActorId.Value) : null;
                responses.Add(new AuditLogResponse
                {
                    AuditLogId = log.Id,
                    UserId = log.ActorId,
                    UserEmail = user?.Email ?? "System",
                    Action = log.Action,
                    Entity = log.EntityType,
                    EntityId = log.EntityId,
                    OldValues = log.BeforeJson ?? "",
                    NewValues = log.AfterJson ?? "",
                    IpAddress = "Unknown",
                    UserAgent = null,
                    CreatedAt = log.CreatedAt
                });
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return [];
        }
    }

    public async Task<ActivitySummaryResponse> GetActivitySummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var logs = await _auditLogRepository.GetAllAsync();
            var filtered = logs.AsEnumerable();

            if (fromDate.HasValue)
            {
                filtered = filtered.Where(l => l.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filtered = filtered.Where(l => l.CreatedAt <= toDate.Value);
            }

            var summary = new ActivitySummaryResponse
            {
                TotalActions = filtered.Count(),
                ActionsByType = filtered
                    .GroupBy(l => l.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ActionsByEntity = filtered
                    .GroupBy(l => l.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentActivities = filtered
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(10)
                    .Select(l => new RecentActivityResponse
                    {
                        Action = l.Action,
                        Entity = l.EntityType,
                        UserEmail = "User",
                        Timestamp = l.CreatedAt
                    })
                    .ToList()
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity summary");
            return new ActivitySummaryResponse();
        }
    }

    public async Task<List<AuditLogResponse>> GetUserActivityAsync(int userId, int days = 30)
    {
        try
        {
            var filter = new AuditLogFilterDto
            {
                UserId = userId,
                FromDate = DateTime.UtcNow.AddDays(-days),
                PageSize = 1000
            };

            return await GetAuditLogsAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity");
            return [];
        }
    }

    public async Task<List<AuditLogResponse>> GetEntityHistoryAsync(string entity, int entityId)
    {
        try
        {
            var logs = await _auditLogRepository.GetAllAsync();
            var filtered = logs
                .Where(l => l.EntityType == entity && l.EntityId == entityId)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();

            var responses = new List<AuditLogResponse>();
            foreach (var log in filtered)
            {
                var user = log.ActorId.HasValue ? await _userRepository.GetByIdAsync(log.ActorId.Value) : null;
                responses.Add(new AuditLogResponse
                {
                    AuditLogId = log.Id,
                    UserId = log.ActorId,
                    UserEmail = user?.Email ?? "System",
                    Action = log.Action,
                    Entity = log.EntityType,
                    EntityId = log.EntityId,
                    OldValues = log.BeforeJson ?? "",
                    NewValues = log.AfterJson ?? "",
                    IpAddress = "Unknown",
                    CreatedAt = log.CreatedAt
                });
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity history");
            return [];
        }
    }
}
