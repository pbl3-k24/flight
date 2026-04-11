using FlightBooking.Application.DTOs.Admin;
using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(
    IAdminFlightService adminFlightService,
    IAdminPricingService adminPricingService,
    IAdminRefundService adminRefundService,
    IAuditLogService auditLogService,
    IPricingService pricingService) : ControllerBase
{
    // Flight management
    [HttpGet("flights")]
    public async Task<IActionResult> GetFlights([FromQuery] AdminFlightFilter filter)
    {
        var flights = await adminFlightService.GetFlightsAsync(filter);
        return Ok(flights);
    }

    [HttpGet("flights/{id}/summary")]
    public async Task<IActionResult> GetFlightSummary(Guid id)
    {
        var summary = await adminFlightService.GetFlightSummaryAsync(id);
        return Ok(summary);
    }

    [HttpPost("flights/bulk-cancel")]
    public async Task<IActionResult> BulkCancelFlights([FromBody] BulkCancelRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await adminFlightService.BulkCancelFlightsAsync(request.FlightIds, request.Reason, adminId);
        return NoContent();
    }

    // Pricing management
    [HttpGet("pricing/rules")]
    public async Task<IActionResult> GetPriceRules()
    {
        var rules = await adminPricingService.GetPriceRulesAsync();
        return Ok(rules);
    }

    [HttpPost("pricing/rules")]
    public async Task<IActionResult> CreatePriceRule([FromBody] CreatePriceRuleRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var rule = await adminPricingService.CreatePriceRuleAsync(request, adminId);
        return Ok(rule);
    }

    [HttpPost("pricing/override")]
    public async Task<IActionResult> OverridePrice([FromBody] PriceOverrideRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await pricingService.OverridePriceAsync(request.FlightId, request.FareClassId, request.NewPrice, request.Reason, adminId);
        return Ok(result);
    }

    [HttpPost("pricing/recalculate")]
    public async Task<IActionResult> TriggerRecalculation([FromBody] Guid? flightId)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await adminPricingService.TriggerPriceRecalculationAsync(flightId, adminId);
        return Ok(new { message = "Price recalculation triggered." });
    }

    // Refund management
    [HttpGet("refunds/pending")]
    public async Task<IActionResult> GetPendingRefunds([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var refunds = await adminRefundService.GetPendingRefundsAsync(page, pageSize);
        return Ok(refunds);
    }

    [HttpGet("refunds/stats")]
    public async Task<IActionResult> GetRefundStats([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var stats = await adminRefundService.GetRefundStatsAsync(from, to);
        return Ok(stats);
    }

    // Audit logs
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilter filter)
    {
        var logs = await auditLogService.GetLogsAsync(filter);
        return Ok(logs);
    }
}

public record BulkCancelRequest(IEnumerable<Guid> FlightIds, string Reason);
public record PriceOverrideRequest(Guid FlightId, Guid FareClassId, decimal NewPrice, string Reason);
