namespace API.Application.Interfaces;

using API.Application.Dtos.Dashboard;

public interface IDashboardService
{
    /// <summary>
    /// Gets dashboard metrics.
    /// </summary>
    Task<DashboardMetricsResponse> GetDashboardMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets system health status.
    /// </summary>
    Task<SystemHealthResponse> GetSystemHealthAsync();

    /// <summary>
    /// Gets revenue analytics.
    /// </summary>
    Task<Dictionary<string, decimal>> GetRevenueAnalyticsAsync(int days = 30);

    /// <summary>
    /// Gets booking analytics.
    /// </summary>
    Task<Dictionary<string, int>> GetBookingAnalyticsAsync(int days = 30);

    /// <summary>
    /// Gets top performing flights.
    /// </summary>
    Task<List<TopFlightResponse>> GetTopFlightsAsync(int days = 30, int limit = 10);

    /// <summary>
    /// Gets user statistics.
    /// </summary>
    Task<Dictionary<string, int>> GetUserStatisticsAsync();
}
