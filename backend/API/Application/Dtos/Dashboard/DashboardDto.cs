namespace API.Application.Dtos.Dashboard;

public class DashboardMetricsResponse
{
    public DashboardSummary Summary { get; set; } = null!;

    public ChartData BookingTrends { get; set; } = null!;

    public ChartData RevenueTrends { get; set; } = null!;

    public List<TopFlightResponse> TopFlights { get; set; } = [];

    public Dictionary<string, int> BookingsByStatus { get; set; } = [];

    public Dictionary<string, decimal> PaymentMethodDistribution { get; set; } = [];
}

public class DashboardSummary
{
    public int TotalBookings { get; set; }

    public int BookingsToday { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TodayRevenue { get; set; }

    public int ActiveUsers { get; set; }

    public int PendingRefunds { get; set; }

    public decimal OccupancyRate { get; set; }

    public int UpcomingFlights { get; set; }
}

public class ChartData
{
    public List<string> Labels { get; set; } = [];

    public List<decimal> Values { get; set; } = [];

    public string Title { get; set; } = null!;
}

public class TopFlightResponse
{
    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string Route { get; set; } = null!;

    public int TotalBookings { get; set; }

    public decimal Revenue { get; set; }

    public decimal OccupancyPercent { get; set; }
}

public class SystemHealthResponse
{
    public string Status { get; set; } = null!; // HEALTHY, WARNING, CRITICAL

    public Dictionary<string, ComponentHealth> Components { get; set; } = [];

    public DateTime LastCheck { get; set; }
}

public class ComponentHealth
{
    public string Name { get; set; } = null!;

    public string Status { get; set; } = null!; // OK, WARNING, ERROR

    public string? Message { get; set; }

    public long ResponseTimeMs { get; set; }
}
