namespace API.Controllers;

using API.Application.Dtos.FlightTemplate;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for managing flight schedule templates
/// Templates are reusable weekly flight patterns that can be used to generate actual flights
/// </summary>
[ApiController]
[Route("api/v1/admin/flight-templates")]
[Authorize(Roles = "Admin")]
public class FlightTemplateController : ControllerBase
{
    private readonly IFlightTemplateService _flightTemplateService;
    private readonly ILogger<FlightTemplateController> _logger;

    public FlightTemplateController(
        IFlightTemplateService flightTemplateService,
        ILogger<FlightTemplateController> logger)
    {
        _flightTemplateService = flightTemplateService ?? throw new ArgumentNullException(nameof(flightTemplateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all flight schedule templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FlightScheduleTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTemplates()
    {
        try
        {
            var templates = await _flightTemplateService.GetAllTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all flight templates");
            return StatusCode(500, new { message = "Error retrieving flight templates" });
        }
    }

    /// <summary>
    /// Get flight schedule template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FlightScheduleTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateById(int id)
    {
        try
        {
            var template = await _flightTemplateService.GetTemplateByIdAsync(id);

            if (template == null)
            {
                return NotFound(new { message = $"Template with ID {id} not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight template {TemplateId}", id);
            return StatusCode(500, new { message = "Error retrieving flight template" });
        }
    }

    /// <summary>
    /// Create a new flight schedule template (save to DB for reuse)
    /// This creates a reusable template that can be used to generate flights later
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FlightScheduleTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateFlightTemplateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await _flightTemplateService.CreateTemplateAsync(dto);

            return CreatedAtAction(
                nameof(GetTemplateById),
                new { id = template.Id },
                template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flight template");
            return StatusCode(500, new { message = "Error creating flight template", error = ex.Message });
        }
    }

    /// <summary>
    /// Update existing flight schedule template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FlightScheduleTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] CreateFlightTemplateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await _flightTemplateService.UpdateTemplateAsync(id, dto);
            return Ok(template);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight template {TemplateId}", id);
            return StatusCode(500, new { message = "Error updating flight template", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete flight schedule template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        try
        {
            var result = await _flightTemplateService.DeleteTemplateAsync(id);

            if (!result)
            {
                return NotFound(new { message = $"Template with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flight template {TemplateId}", id);
            return StatusCode(500, new { message = "Error deleting flight template" });
        }
    }

    /// <summary>
    /// Generate actual flights from a saved template for specified week(s)
    /// This is the main function that creates real Flight records in the database
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateFlightsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateFlights([FromBody] GenerateFlightsFromTemplateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _flightTemplateService.GenerateFlightsFromTemplateAsync(dto);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flights from template");
            return StatusCode(500, new { message = "Error generating flights", error = ex.Message });
        }
    }
}
