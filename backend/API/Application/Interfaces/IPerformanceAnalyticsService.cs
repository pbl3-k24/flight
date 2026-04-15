namespace API.Application.Interfaces;

public interface IPerformanceAnalyticsService
{
    /// <summary>
    /// Records API call metrics.
    /// </summary>
    Task RecordApiMetricAsync(string endpoint, long durationMs, int statusCode);

    /// <summary>
    /// Gets API performance statistics.
    /// </summary>
    Task<Dictionary<string, object>> GetApiMetricsAsync(int days = 30);

    /// <summary>
    /// Gets slowest endpoints.
    /// </summary>
    Task<List<Dictionary<string, object>>> GetSlowestEndpointsAsync(int limit = 10);

    /// <summary>
    /// Gets error rate metrics.
    /// </summary>
    Task<Dictionary<string, decimal>> GetErrorRateMetricsAsync();
}
