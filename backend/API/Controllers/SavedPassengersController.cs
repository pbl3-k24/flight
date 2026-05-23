namespace API.Controllers;

using API.Application.Dtos.Passenger;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/saved-passengers")]
[Authorize]
public class SavedPassengersController : ControllerBase
{
    private readonly ISavedPassengerService _savedPassengerService;
    private readonly ILogger<SavedPassengersController> _logger;

    public SavedPassengersController(
        ISavedPassengerService savedPassengerService,
        ILogger<SavedPassengersController> logger)
    {
        _savedPassengerService = savedPassengerService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SavedPassengerResponse>>> GetMySavedPassengersAsync()
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            var result = await _savedPassengerService.GetMyPassengersAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved passengers");
            return StatusCode(500, new { message = "An error occurred while retrieving saved passengers" });
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SavedPassengerResponse>> CreateAsync([FromBody] CreateSavedPassengerDto dto)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            var result = await _savedPassengerService.CreateAsync(userId, dto);
            return Ok(result);
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
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
            _logger.LogError(ex, "Error creating saved passenger");
            return StatusCode(500, new { message = "An error occurred while creating saved passenger" });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedPassengerResponse>> UpdateAsync(int id, [FromBody] UpdateSavedPassengerDto dto)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            var result = await _savedPassengerService.UpdateAsync(userId, id, dto);
            return Ok(result);
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
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
            _logger.LogError(ex, "Error updating saved passenger {PassengerId}", id);
            return StatusCode(500, new { message = "An error occurred while updating saved passenger" });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            var success = await _savedPassengerService.DeleteAsync(userId, id);
            return success ? Ok(new { message = "Saved passenger deleted successfully" }) : BadRequest();
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
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
            _logger.LogError(ex, "Error deleting saved passenger {PassengerId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting saved passenger" });
        }
    }
}

