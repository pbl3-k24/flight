namespace API.Controllers;

using API.Application.Dtos.Reporting;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportingService reportingService,
        ILogger<ReportsController> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new report asynchronously.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportResponse>> GenerateReportAsync([FromBody] ReportRequestDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Generating {ReportType} report", dto.ReportType);
            var report = await _reportingService.GenerateReportAsync(dto, userId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, new { message = "Error generating report" });
        }
    }

    /// <summary>
    /// Gets report status.
    /// </summary>
    [HttpGet("{reportId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportResponse>> GetReportStatusAsync(int reportId)
    {
        try
        {
            var report = await _reportingService.GetReportStatusAsync(reportId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report status");
            return StatusCode(500, new { message = "Error fetching report" });
        }
    }

    /// <summary>
    /// Downloads a generated report.
    /// </summary>
    [HttpGet("{reportId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadReportAsync(int reportId)
    {
        try
        {
            var fileBytes = await _reportingService.DownloadReportAsync(reportId);
            return File(fileBytes, "application/pdf", $"report-{reportId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading report");
            return StatusCode(500, new { message = "Error downloading report" });
        }
    }

    /// <summary>
    /// Gets booking report.
    /// </summary>
    [HttpGet("booking-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingReportDto>> GetBookingReportAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var report = await _reportingService.GetBookingReportAsync(startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking report");
            return StatusCode(500, new { message = "Error fetching report" });
        }
    }

    /// <summary>
    /// Gets revenue report.
    /// </summary>
    [HttpGet("revenue-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueReportDto>> GetRevenueReportAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var report = await _reportingService.GetRevenueReportAsync(startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue report");
            return StatusCode(500, new { message = "Error fetching report" });
        }
    }

    /// <summary>
    /// Gets user report.
    /// </summary>
    [HttpGet("user-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserReportDto>> GetUserReportAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var report = await _reportingService.GetUserReportAsync(startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user report");
            return StatusCode(500, new { message = "Error fetching report" });
        }
    }
}
