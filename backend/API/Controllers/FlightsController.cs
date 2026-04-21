namespace API.Controllers;

using API.Application.Dtos.Flight;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;
    private readonly ILogger<FlightsController> _logger;

    public FlightsController(
        IFlightService flightService,
        ILogger<FlightsController> logger)
    {
        _flightService = flightService;
        _logger = logger;
    }

    /// <summary>
    /// Searches for available flights.
    /// </summary>
    /// <param name="dto">Flight search criteria</param>
    /// <returns>List of available flights</returns>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<FlightSearchResponse>>> SearchFlightsAsync([FromBody] FlightSearchDto dto)
    {
        try
        {
            _logger.LogInformation("Flight search requested: {Departure} -> {Arrival} on {Date}",
                dto.DepartureAirportId, dto.ArrivalAirportId, dto.DepartureDate.Date);

            var flights = await _flightService.SearchAsync(dto);
            return Ok(flights);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Flight search validation error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flight search error");
            return StatusCode(500, new { message = "An error occurred while searching flights" });
        }
    }

    /// <summary>
    /// Gets detailed information about a flight.
    /// </summary>
    /// <param name="id">Flight ID</param>
    /// <returns>Flight details with seat inventory</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlightDetailResponse>> GetFlightAsync(int id)
    {
        try
        {
            _logger.LogInformation("Flight details requested for flight {FlightId}", id);

            var flight = await _flightService.GetFlightAsync(id);
            return Ok(flight);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Flight not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight details");
            return StatusCode(500, new { message = "An error occurred while retrieving flight details" });
        }
    }

    /// <summary>
    /// Gets available seats for a flight and seat class.
    /// </summary>
    /// <param name="flightId">Flight ID</param>
    /// <param name="seatClassId">Seat class ID</param>
    /// <returns>Number of available seats</returns>
    [HttpGet("{flightId}/seats/{seatClassId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> GetAvailableSeatsAsync(int flightId, int seatClassId)
    {
        try
        {
            _logger.LogInformation("Available seats requested for flight {FlightId}, class {SeatClassId}",
                flightId, seatClassId);

            var availableSeats = await _flightService.GetAvailableSeatsAsync(flightId, seatClassId);
            return Ok(availableSeats);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available seats");
            return StatusCode(500, new { message = "An error occurred while checking seat availability" });
        }
    }
}
