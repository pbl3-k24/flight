namespace API.Controllers;

using API.Application.Dtos.FlightDefinition;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/flight-definitions")]
[Authorize(Roles = "Admin")]
public class FlightDefinitionsController : ControllerBase
{
    private readonly IFlightDefinitionService _flightDefinitionService;
    private readonly ILogger<FlightDefinitionsController> _logger;

    public FlightDefinitionsController(
        IFlightDefinitionService flightDefinitionService,
        ILogger<FlightDefinitionsController> logger)
    {
        _flightDefinitionService = flightDefinitionService ?? throw new ArgumentNullException(nameof(flightDefinitionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<FlightDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        try
        {
            var definitions = activeOnly 
                ? await _flightDefinitionService.GetActiveAsync()
                : await _flightDefinitionService.GetAllAsync();
            
            return Ok(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight definitions");
            return StatusCode(500, new { message = "Error retrieving flight definitions" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FlightDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var definition = await _flightDefinitionService.GetByIdAsync(id);
            if (definition == null)
            {
                return NotFound(new { message = $"Flight definition {id} not found" });
            }

            return Ok(definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight definition {Id}", id);
            return StatusCode(500, new { message = "Error retrieving flight definition" });
        }
    }

    [HttpGet("by-flight-number/{flightNumber}")]
    [ProducesResponseType(typeof(FlightDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByFlightNumber(string flightNumber)
    {
        try
        {
            var definition = await _flightDefinitionService.GetByFlightNumberAsync(flightNumber);
            if (definition == null)
            {
                return NotFound(new { message = $"Flight number {flightNumber} not found" });
            }

            return Ok(definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight definition by number {FlightNumber}", flightNumber);
            return StatusCode(500, new { message = "Error retrieving flight definition" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FlightDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFlightDefinitionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _flightDefinitionService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = definition.Id },
                definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flight definition");
            return StatusCode(500, new { message = "Error creating flight definition", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FlightDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFlightDefinitionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _flightDefinitionService.UpdateAsync(id, dto);
            return Ok(definition);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight definition {Id}", id);
            return StatusCode(500, new { message = "Error updating flight definition", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _flightDefinitionService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Flight definition {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flight definition {Id}", id);
            return StatusCode(500, new { message = "Error deleting flight definition" });
        }
    }

    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            var result = await _flightDefinitionService.ActivateAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Flight definition {id} not found" });
            }

            return Ok(new { message = "Flight definition activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating flight definition {Id}", id);
            return StatusCode(500, new { message = "Error activating flight definition" });
        }
    }

    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var result = await _flightDefinitionService.DeactivateAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Flight definition {id} not found" });
            }

            return Ok(new { message = "Flight definition deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating flight definition {Id}", id);
            return StatusCode(500, new { message = "Error deactivating flight definition" });
        }
    }
}
