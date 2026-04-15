namespace API.Controllers;

using API.Application.Dtos.RealTime;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class RealtimeDashboardController : ControllerBase
{
    private readonly IRealtimeDashboardService _realtimeService;
    private readonly IPerformanceAnalyticsService _performanceService;
    private readonly ILogger<RealtimeDashboardController> _logger;

    public RealtimeDashboardController(
        IRealtimeDashboardService realtimeService,
        IPerformanceAnalyticsService performanceService,
        ILogger<RealtimeDashboardController> logger)
    {
        _realtimeService = realtimeService;
        _performanceService = performanceService;
        _logger = logger;
    }

    /// <summary>
    /// Gets current realtime metrics.
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RealtimeMetricDto>> GetMetricsAsync()
    {
        try
        {
            var metrics = await _realtimeService.GetRealtimeMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realtime metrics");
            return StatusCode(500, new { message = "Error fetching metrics" });
        }
    }

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AlertDto>>> GetAlertsAsync()
    {
        try
        {
            var alerts = await _realtimeService.GetActiveAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(500, new { message = "Error fetching alerts" });
        }
    }

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    [HttpPost("alerts/{alertId}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AcknowledgeAlertAsync(int alertId)
    {
        try
        {
            var success = await _realtimeService.AcknowledgeAlertAsync(alertId);
            return success ? Ok(new { message = "Alert acknowledged" }) : BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert");
            return StatusCode(500, new { message = "Error updating alert" });
        }
    }

    /// <summary>
    /// Gets performance metrics.
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, object>>> GetPerformanceMetricsAsync()
    {
        try
        {
            var metrics = await _realtimeService.GetPerformanceMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { message = "Error fetching metrics" });
        }
    }

    /// <summary>
    /// Gets database statistics.
    /// </summary>
    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, long>>> GetDatabaseStatsAsync()
    {
        try
        {
            var stats = await _realtimeService.GetDatabaseStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database stats");
            return StatusCode(500, new { message = "Error fetching database stats" });
        }
    }

    /// <summary>
    /// Gets API performance analytics.
    /// </summary>
    [HttpGet("api-analytics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, object>>> GetApiAnalyticsAsync([FromQuery] int days = 30)
    {
        try
        {
            var analytics = await _performanceService.GetApiMetricsAsync(days);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API analytics");
            return StatusCode(500, new { message = "Error fetching analytics" });
        }
    }

    /// <summary>
    /// Gets slowest endpoints.
    /// </summary>
    [HttpGet("slowest-endpoints")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Dictionary<string, object>>>> GetSlowestEndpointsAsync([FromQuery] int limit = 10)
    {
        try
        {
            var endpoints = await _performanceService.GetSlowestEndpointsAsync(limit);
            return Ok(endpoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slowest endpoints");
            return StatusCode(500, new { message = "Error fetching data" });
        }
    }

    /// <summary>
    /// Gets error rate metrics.
    /// </summary>
    [HttpGet("error-rates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetErrorRatesAsync()
    {
        try
        {
            var errorRates = await _performanceService.GetErrorRateMetricsAsync();
            return Ok(errorRates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error rates");
            return StatusCode(500, new { message = "Error fetching metrics" });
        }
    }
}
