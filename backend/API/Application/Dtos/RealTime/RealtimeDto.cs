namespace API.Application.Dtos.RealTime;

public class DashboardUpdateDto
{
    public string UpdateType { get; set; } = null!; // BOOKING, PAYMENT, REFUND, USER

    public object Data { get; set; } = null!;

    public DateTime Timestamp { get; set; }
}

public class RealtimeMetricDto
{
    public int ActiveUsers { get; set; }

    public int BookingsInProgress { get; set; }

    public int PaymentsProcessing { get; set; }

    public int PendingRefunds { get; set; }

    public decimal TodayRevenue { get; set; }

    public int TodayBookings { get; set; }

    public double SystemLoad { get; set; }

    public DateTime LastUpdated { get; set; }
}

public class AlertDto
{
    public int AlertId { get; set; }

    public string Type { get; set; } = null!; // WARNING, ERROR, INFO

    public string Message { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Acknowledged { get; set; }
}
