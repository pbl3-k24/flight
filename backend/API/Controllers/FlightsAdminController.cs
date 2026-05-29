namespace API.Controllers;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class FlightsAdminController : ControllerBase
{
    private readonly IFlightAdminService _flightService;
    private readonly ILogger<FlightsAdminController> _logger;

    public FlightsAdminController(
        IFlightAdminService flightService,
        ILogger<FlightsAdminController> logger)
    {
        _flightService = flightService;
        _logger = logger;
    }

    /// <summary>
    /// Creates schedule for one specific week and auto-generates the same pattern for following weeks.
    /// Default auto-generation: 12 weeks.
    /// </summary>
    [HttpPost("weekly-schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<FlightManagementResponse>>> CreateWeeklyScheduleAsync([FromBody] CreateWeeklyScheduleDto dto)
    {
        try
        {
            var response = await _flightService.CreateWeeklyScheduleAsync(dto);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating weekly schedule");
            return StatusCode(500, new { message = "Error creating weekly schedule" });
        }
    }

    /// <summary>
    /// Creates a new flight (Admin only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FlightManagementResponse>> CreateFlightAsync([FromBody] CreateFlightDto dto)
    {
        try
        {
            _logger.LogInformation("Creating flight from definition {FlightDefinitionId}", dto.FlightDefinitionId);
            var response = await _flightService.CreateFlightAsync(dto);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flight");
            return StatusCode(500, new { message = "Error creating flight" });
        }
    }

    /// <summary>
    /// Updates a flight (Admin only).
    /// </summary>
    [HttpPut("{flightId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFlightAsync(int flightId, [FromBody] UpdateFlightDto dto)
    {
        try
        {
            _logger.LogInformation("Updating flight: {FlightId}", flightId);
            var success = await _flightService.UpdateFlightAsync(flightId, dto);
            return success ? Ok(new { message = "Flight updated successfully" }) : BadRequest();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight");
            return StatusCode(500, new { message = "Error updating flight" });
        }
    }

    [HttpPut("{flightId}/prices")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFlightPricesAsync(int flightId, [FromBody] UpdateFlightPricesDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? adminUserId = null;
            if (int.TryParse(userIdClaim, out var parsed))
            {
                adminUserId = parsed;
            }

            var success = await _flightService.UpdateFlightPricesAsync(flightId, dto, adminUserId);
            return success ? Ok(new { message = "Flight prices updated successfully" }) : BadRequest();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight prices");
            return StatusCode(500, new { message = "Error updating flight prices" });
        }
    }

    /// <summary>
    /// Deletes a flight (soft delete) (Admin only).
    /// </summary>
    [HttpDelete("{flightId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFlightAsync(int flightId)
    {
        try
        {
            _logger.LogInformation("Deleting flight: {FlightId}", flightId);
            var success = await _flightService.DeleteFlightAsync(flightId);
            return success ? Ok(new { message = "Flight deleted successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flight");
            return StatusCode(500, new { message = "Error deleting flight" });
        }
    }

    /// <summary>
    /// Cancels a flight and all affected bookings, queues refunds for paid bookings,
    /// and sends cancellation emails (Admin only).
    /// </summary>
    [HttpPost("{flightId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CancelFlightAdminResponse>> CancelFlightAsync(int flightId, [FromBody] CancelFlightAdminDto dto)
    {
        try
        {
            _logger.LogInformation("Cancelling flight by admin: {FlightId}", flightId);
            var response = await _flightService.CancelFlightAsync(flightId, dto);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling flight");
            return StatusCode(500, new { message = "Error cancelling flight" });
        }
    }

    /// <summary>
    /// Gets all flights (Admin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FlightManagementResponse>>> GetFlightsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var response = await _flightService.GetFlightsAsync(page, pageSize);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flights");
            return StatusCode(500, new { message = "Error getting flights" });
        }
    }

    /// <summary>
    /// Gets all flights in a specific date (Admin only).
    /// </summary>
    [HttpGet("by-date")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<FlightManagementResponse>>> GetFlightsByDateAsync([FromQuery] DateOnly date)
    {
        try
        {
            var response = await _flightService.GetFlightsByDateAsync(date);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flights by date");
            return StatusCode(500, new { message = "Error getting flights by date" });
        }
    }

    /// <summary>
    /// Creates a new route (Admin only).
    /// </summary>
    [HttpPost("routes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RouteManagementResponse>> CreateRouteAsync([FromBody] CreateRouteDto dto)
    {
        try
        {
            _logger.LogInformation("Creating route");
            var response = await _flightService.CreateRouteAsync(dto);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating route");
            return StatusCode(500, new { message = "Error creating route" });
        }
    }

    /// <summary>
    /// Updates a route (Admin only).
    /// </summary>
    [HttpPut("routes/{routeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRouteAsync(int routeId, [FromBody] UpdateRouteDto dto)
    {
        try
        {
            _logger.LogInformation("Updating route: {RouteId}", routeId);
            var success = await _flightService.UpdateRouteAsync(routeId, dto);
            return success ? Ok(new { message = "Route updated successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating route");
            return StatusCode(500, new { message = "Error updating route" });
        }
    }

    /// <summary>
    /// Gets all routes (Admin only).
    /// </summary>
    [HttpGet("routes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RouteManagementResponse>>> GetRoutesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var response = await _flightService.GetRoutesAsync(page, pageSize);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting routes");
            return StatusCode(500, new { message = "Error getting routes" });
        }
    }
}
