namespace API.Application.Services;

using API.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private readonly ILogger<PerformanceAnalyticsService> _logger;
    private static readonly Dictionary<string, List<(DateTime timestamp, long durationMs, int statusCode)>> ApiMetrics = new();

    public PerformanceAnalyticsService(ILogger<PerformanceAnalyticsService> logger)
    {
        _logger = logger;
    }

    public async Task RecordApiMetricAsync(string endpoint, long durationMs, int statusCode)
    {
        try
        {
            if (!ApiMetrics.ContainsKey(endpoint))
            {
                ApiMetrics[endpoint] = new();
            }

            ApiMetrics[endpoint].Add((DateTime.UtcNow, durationMs, statusCode));

            // Keep only last 1000 metrics per endpoint
            if (ApiMetrics[endpoint].Count > 1000)
            {
                ApiMetrics[endpoint].RemoveRange(0, ApiMetrics[endpoint].Count - 1000);
            }

            _logger.LogInformation("API metric recorded: {Endpoint} {Duration}ms", endpoint, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording API metric");
        }
    }

    public async Task<Dictionary<string, object>> GetApiMetricsAsync(int days = 30)
    {
        try
        {
            var metrics = new Dictionary<string, object>();

            foreach (var endpoint in ApiMetrics.Keys)
            {
                var recent = ApiMetrics[endpoint].Where(m => m.timestamp > DateTime.UtcNow.AddDays(-days)).ToList();

                metrics[endpoint] = new
                {
                    CallCount = recent.Count,
                    AverageDuration = recent.Count > 0 ? recent.Average(m => m.durationMs) : 0,
                    MaxDuration = recent.Count > 0 ? recent.Max(m => m.durationMs) : 0,
                    MinDuration = recent.Count > 0 ? recent.Min(m => m.durationMs) : 0,
                    ErrorCount = recent.Count(m => m.statusCode >= 400)
                };
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API metrics");
            return new Dictionary<string, object>();
        }
    }

    public async Task<List<Dictionary<string, object>>> GetSlowestEndpointsAsync(int limit = 10)
    {
        try
        {
            var slowest = new List<Dictionary<string, object>>();

            var sorted = ApiMetrics
                .Select(kvp => new
                {
                    Endpoint = kvp.Key,
                    AvgDuration = kvp.Value.Count > 0 ? kvp.Value.Average(m => m.durationMs) : 0
                })
                .OrderByDescending(x => x.AvgDuration)
                .Take(limit)
                .ToList();

            foreach (var item in sorted)
            {
                slowest.Add(new Dictionary<string, object>
                {
                    { "Endpoint", item.Endpoint },
                    { "AverageDuration", item.AvgDuration }
                });
            }

            return slowest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slowest endpoints");
            return new List<Dictionary<string, object>>();
        }
    }

    public async Task<Dictionary<string, decimal>> GetErrorRateMetricsAsync()
    {
        try
        {
            var errorRates = new Dictionary<string, decimal>();

            foreach (var endpoint in ApiMetrics.Keys)
            {
                var metrics = ApiMetrics[endpoint];
                var total = metrics.Count;
                var errors = metrics.Count(m => m.statusCode >= 400);

                errorRates[endpoint] = total > 0 ? (decimal)errors / total : 0;
            }

            return errorRates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error rate metrics");
            return new Dictionary<string, decimal>();
        }
    }
}
