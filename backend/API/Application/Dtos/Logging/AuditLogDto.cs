namespace API.Application.Dtos.Logging;

public class AuditLogResponse
{
    public int AuditLogId { get; set; }

    public int? UserId { get; set; }

    public string UserEmail { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public int? EntityId { get; set; }

    public string OldValues { get; set; } = null!;

    public string NewValues { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AuditLogFilterDto
{
    public int? UserId { get; set; }

    public string? Entity { get; set; }

    public string? Action { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}

public class ActivitySummaryResponse
{
    public int TotalActions { get; set; }

    public Dictionary<string, int> ActionsByType { get; set; } = [];

    public Dictionary<string, int> ActionsByEntity { get; set; } = [];

    public List<RecentActivityResponse> RecentActivities { get; set; } = [];
}

public class RecentActivityResponse
{
    public string Action { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public DateTime Timestamp { get; set; }
}
