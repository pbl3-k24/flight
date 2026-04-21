namespace API.Controllers;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class PromotionsAdminController : ControllerBase
{
    private readonly IPromotionAdminService _promotionService;
    private readonly ILogger<PromotionsAdminController> _logger;

    public PromotionsAdminController(
        IPromotionAdminService promotionService,
        ILogger<PromotionsAdminController> logger)
    {
        _promotionService = promotionService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new promotion (Admin only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionManagementResponse>> CreatePromotionAsync([FromBody] CreatePromotionDto dto)
    {
        try
        {
            _logger.LogInformation("Creating promotion: {Code}", dto.Code);
            var response = await _promotionService.CreatePromotionAsync(dto);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion");
            return StatusCode(500, new { message = "Error creating promotion" });
        }
    }

    /// <summary>
    /// Updates a promotion (Admin only).
    /// </summary>
    [HttpPut("{promotionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePromotionAsync(int promotionId, [FromBody] UpdatePromotionDto dto)
    {
        try
        {
            _logger.LogInformation("Updating promotion: {PromotionId}", promotionId);
            var success = await _promotionService.UpdatePromotionAsync(promotionId, dto);
            return success ? Ok(new { message = "Promotion updated successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion");
            return StatusCode(500, new { message = "Error updating promotion" });
        }
    }

    /// <summary>
    /// Deactivates a promotion (Admin only).
    /// </summary>
    [HttpDelete("{promotionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivatePromotionAsync(int promotionId)
    {
        try
        {
            _logger.LogInformation("Deactivating promotion: {PromotionId}", promotionId);
            var success = await _promotionService.DeactivatePromotionAsync(promotionId);
            return success ? Ok(new { message = "Promotion deactivated successfully" }) : BadRequest();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating promotion");
            return StatusCode(500, new { message = "Error deactivating promotion" });
        }
    }

    /// <summary>
    /// Gets all promotions (Admin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromotionManagementResponse>>> GetPromotionsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Getting promotions");
            var response = await _promotionService.GetPromotionsAsync(page, pageSize);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotions");
            return StatusCode(500, new { message = "Error getting promotions" });
        }
    }

    /// <summary>
    /// Gets active promotions (Admin only).
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromotionManagementResponse>>> GetActivePromotionsAsync()
    {
        try
        {
            _logger.LogInformation("Getting active promotions");
            var response = await _promotionService.GetActivePromotionsAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active promotions");
            return StatusCode(500, new { message = "Error getting active promotions" });
        }
    }

    /// <summary>
    /// Gets promotion usage statistics (Admin only).
    /// </summary>
    [HttpGet("{promotionId}/usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Dictionary<string, int>>> GetPromotionUsageAsync(int promotionId)
    {
        try
        {
            _logger.LogInformation("Getting promotion usage: {PromotionId}", promotionId);
            var response = await _promotionService.GetPromotionUsageAsync(promotionId);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion usage");
            return StatusCode(500, new { message = "Error getting promotion usage" });
        }
    }
}
