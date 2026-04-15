namespace API.Application.Interfaces;

using API.Application.Dtos.RealTime;

public interface IRealtimeDashboardService
{
    /// <summary>
    /// Gets current realtime metrics.
    /// </summary>
    Task<RealtimeMetricDto> GetRealtimeMetricsAsync();

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    Task<List<AlertDto>> GetActiveAlertsAsync();

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(int alertId);

    /// <summary>
    /// Gets system performance metrics.
    /// </summary>
    Task<Dictionary<string, object>> GetPerformanceMetricsAsync();

    /// <summary>
    /// Gets database statistics.
    /// </summary>
    Task<Dictionary<string, long>> GetDatabaseStatsAsync();
}

public interface IRealtimeNotificationHub
{
    /// <summary>
    /// Sends update to connected clients.
    /// </summary>
    Task SendDashboardUpdateAsync(string message);

    /// <summary>
    /// Sends alert to connected clients.
    /// </summary>
    Task SendAlertAsync(string alertType, string message);

    /// <summary>
    /// Broadcasts metrics update.
    /// </summary>
    Task BroadcastMetricsAsync(object metrics);
}
