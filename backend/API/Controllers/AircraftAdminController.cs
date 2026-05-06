namespace API.Controllers;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for managing aircraft (Admin only)
/// </summary>
[ApiController]
[Route("api/v1/admin/aircraft")]
[Authorize(Roles = "Admin")]
public class AircraftAdminController : ControllerBase
{
    private readonly IAircraftAdminService _aircraftService;
    private readonly ILogger<AircraftAdminController> _logger;

    public AircraftAdminController(
        IAircraftAdminService aircraftService,
        ILogger<AircraftAdminController> logger)
    {
        _aircraftService = aircraftService ?? throw new ArgumentNullException(nameof(aircraftService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all aircraft with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AircraftManagementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAircraft(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false)
    {
        try
        {
            var aircraft = await _aircraftService.GetAllAircraftAsync(page, pageSize, includeDeleted);
            return Ok(aircraft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft");
            return StatusCode(500, new { message = "Error retrieving aircraft" });
        }
    }

    /// <summary>
    /// Get aircraft by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AircraftManagementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAircraftById(int id)
    {
        try
        {
            var aircraft = await _aircraftService.GetAircraftByIdAsync(id);
            if (aircraft == null)
            {
                return NotFound(new { message = $"Aircraft {id} not found" });
            }
            return Ok(aircraft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft {AircraftId}", id);
            return StatusCode(500, new { message = "Error retrieving aircraft" });
        }
    }

    /// <summary>
    /// Create new aircraft
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AircraftManagementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAircraft([FromBody] CreateAircraftDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var aircraft = await _aircraftService.CreateAircraftAsync(dto);
            return CreatedAtAction(nameof(GetAircraftById), new { id = aircraft.AircraftId }, aircraft);
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
            _logger.LogError(ex, "Error creating aircraft");
            return StatusCode(500, new { message = "Error creating aircraft", error = ex.Message });
        }
    }

    /// <summary>
    /// Update aircraft
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AircraftManagementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAircraft(int id, [FromBody] UpdateAircraftDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var aircraft = await _aircraftService.UpdateAircraftAsync(id, dto);
            return Ok(aircraft);
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
            _logger.LogError(ex, "Error updating aircraft {AircraftId}", id);
            return StatusCode(500, new { message = "Error updating aircraft", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete aircraft (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAircraft(int id)
    {
        try
        {
            var result = await _aircraftService.DeleteAircraftAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Aircraft {id} not found" });
            }
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting aircraft {AircraftId}", id);
            return StatusCode(500, new { message = "Error deleting aircraft" });
        }
    }

    /// <summary>
    /// Restore soft-deleted aircraft
    /// </summary>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreAircraft(int id)
    {
        try
        {
            var result = await _aircraftService.RestoreAircraftAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Aircraft {id} not found" });
            }
            return Ok(new { message = "Aircraft restored successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring aircraft {AircraftId}", id);
            return StatusCode(500, new { message = "Error restoring aircraft" });
        }
    }

    /// <summary>
    /// Get aircraft statistics
    /// </summary>
    [HttpGet("{id}/statistics")]
    [ProducesResponseType(typeof(AircraftStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAircraftStatistics(int id)
    {
        try
        {
            var statistics = await _aircraftService.GetAircraftStatisticsAsync(id);
            return Ok(statistics);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft statistics {AircraftId}", id);
            return StatusCode(500, new { message = "Error retrieving statistics" });
        }
    }
}
