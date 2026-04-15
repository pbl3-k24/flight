namespace API.Controllers;

using API.Application.Dtos.Dashboard;
using API.Application.Dtos.Logging;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IAuditLogService _auditLogService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        IAuditLogService auditLogService,
        IBackgroundJobService backgroundJobService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _auditLogService = auditLogService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Gets dashboard metrics and analytics.
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardMetricsResponse>> GetMetricsAsync(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            _logger.LogInformation("Fetching dashboard metrics");
            var metrics = await _dashboardService.GetDashboardMetricsAsync(fromDate, toDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard metrics");
            return StatusCode(500, new { message = "Error fetching metrics" });
        }
    }

    /// <summary>
    /// Gets system health status.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthResponse>> GetHealthAsync()
    {
        try
        {
            _logger.LogInformation("Checking system health");
            var health = await _dashboardService.GetSystemHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return StatusCode(500, new { message = "Error checking health" });
        }
    }

    /// <summary>
    /// Gets revenue analytics.
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetRevenueAsync(
        [FromQuery] int days = 30)
    {
        try
        {
            _logger.LogInformation("Fetching revenue analytics");
            var revenue = await _dashboardService.GetRevenueAnalyticsAsync(days);
            return Ok(revenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return StatusCode(500, new { message = "Error fetching analytics" });
        }
    }

    /// <summary>
    /// Gets booking analytics.
    /// </summary>
    [HttpGet("bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, int>>> GetBookingAnalyticsAsync(
        [FromQuery] int days = 30)
    {
        try
        {
            _logger.LogInformation("Fetching booking analytics");
            var analytics = await _dashboardService.GetBookingAnalyticsAsync(days);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking analytics");
            return StatusCode(500, new { message = "Error fetching analytics" });
        }
    }

    /// <summary>
    /// Gets audit logs with filters.
    /// </summary>
    [HttpPost("audit-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AuditLogResponse>>> GetAuditLogsAsync([FromBody] AuditLogFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Fetching audit logs");
            var logs = await _auditLogService.GetAuditLogsAsync(filter);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, new { message = "Error fetching audit logs" });
        }
    }

    /// <summary>
    /// Gets activity summary.
    /// </summary>
    [HttpGet("activity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivitySummaryResponse>> GetActivityAsync(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            _logger.LogInformation("Fetching activity summary");
            var summary = await _auditLogService.GetActivitySummaryAsync(fromDate, toDate);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity summary");
            return StatusCode(500, new { message = "Error fetching activity" });
        }
    }

    /// <summary>
    /// Gets background job status.
    /// </summary>
    [HttpGet("jobs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, string>>> GetJobStatusAsync()
    {
        try
        {
            _logger.LogInformation("Fetching background job status");
            var status = await _backgroundJobService.GetJobStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status");
            return StatusCode(500, new { message = "Error fetching job status" });
        }
    }
}
