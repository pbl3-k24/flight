namespace API.Controllers;

using API.Application.Dtos.Search;
using API.Application.Exceptions;
using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IAdvancedSearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IAdvancedSearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Searches flights with advanced filters.
    /// </summary>
    [HttpPost("flights")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResultDto<dynamic>>> SearchFlightsAsync([FromBody] AdvancedSearchFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Searching flights");
            var results = await _searchService.SearchFlightsAsync(filter);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching flights");
            return StatusCode(500, new { message = "Error searching" });
        }
    }

    /// <summary>
    /// Searches bookings with advanced filters.
    /// </summary>
    [HttpPost("bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResultDto<dynamic>>> SearchBookingsAsync([FromBody] AdvancedSearchFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Searching bookings");
            var results = await _searchService.SearchBookingsAsync(filter);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching bookings");
            return StatusCode(500, new { message = "Error searching" });
        }
    }

    /// <summary>
    /// Searches users with advanced filters (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResultDto<dynamic>>> SearchUsersAsync([FromBody] AdvancedSearchFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Searching users");
            var results = await _searchService.SearchUsersAsync(filter);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { message = "Error searching" });
        }
    }

    /// <summary>
    /// Performs global search across entities.
    /// </summary>
    [HttpGet("global")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, object>>> GlobalSearchAsync([FromQuery] string searchTerm)
    {
        try
        {
            _logger.LogInformation("Global search: {SearchTerm}", searchTerm);
            var results = await _searchService.GlobalSearchAsync(searchTerm);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in global search");
            return StatusCode(500, new { message = "Error searching" });
        }
    }
}
