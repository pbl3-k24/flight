namespace API.Controllers;

using API.Application.Dtos.Promotion;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger<PromotionsController> _logger;

    public PromotionsController(
        IPromotionService promotionService,
        ILogger<PromotionsController> logger)
    {
        _promotionService = promotionService;
        _logger = logger;
    }

    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AvailablePromotionResponse>>> GetAvailableAsync()
    {
        try
        {
            var promotions = await _promotionService.GetAvailablePromotionsAsync();
            return Ok(promotions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available promotions");
            return StatusCode(500, new { message = "An error occurred while retrieving available promotions" });
        }
    }
}

