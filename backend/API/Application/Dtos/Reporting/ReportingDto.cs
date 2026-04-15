namespace API.Application.Dtos.Reporting;

public class ReportRequestDto
{
    public string ReportType { get; set; } = null!; // BOOKING, REVENUE, USER, FLIGHT, REFUND

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Format { get; set; } = "PDF"; // PDF, Excel, CSV, JSON
}

public class ReportResponse
{
    public int ReportId { get; set; }

    public string ReportType { get; set; } = null!;

    public string Format { get; set; } = null!;

    public string Status { get; set; } = null!; // PENDING, READY, FAILED

    public string? FileUrl { get; set; }

    public long? FileSize { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public int? GeneratedByUserId { get; set; }
}

public class BookingReportDto
{
    public int TotalBookings { get; set; }

    public decimal TotalRevenue { get; set; }

    public int ConfirmedBookings { get; set; }

    public int PendingBookings { get; set; }

    public int CancelledBookings { get; set; }

    public decimal AverageBookingValue { get; set; }

    public List<DailyBookingMetric> DailyMetrics { get; set; } = [];
}

public class DailyBookingMetric
{
    public DateTime Date { get; set; }

    public int Count { get; set; }

    public decimal Revenue { get; set; }

    public decimal AverageValue { get; set; }
}

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }

    public decimal AverageTransactionValue { get; set; }

    public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = [];

    public Dictionary<string, decimal> RevenueByRoute { get; set; } = [];

    public List<DailyRevenueMetric> DailyMetrics { get; set; } = [];
}

public class DailyRevenueMetric
{
    public DateTime Date { get; set; }

    public decimal Amount { get; set; }

    public int TransactionCount { get; set; }
}

public class UserReportDto
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int NewUsersThisPeriod { get; set; }

    public int UsersWithBookings { get; set; }

    public decimal AverageSpendPerUser { get; set; }

    public List<UserSegmentMetric> Segments { get; set; } = [];
}

public class UserSegmentMetric
{
    public string Segment { get; set; } = null!;

    public int Count { get; set; }

    public decimal AverageSpend { get; set; }
}
