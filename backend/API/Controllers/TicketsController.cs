namespace API.Controllers;

using API.Application.Dtos.Ticket;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketService ticketService,
        ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a ticket by ticket number.
    /// </summary>
    [HttpGet("{ticketNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> GetTicketAsync(string ticketNumber)
    {
        try
        {
            _logger.LogInformation("Ticket requested: {TicketNumber}", ticketNumber);

            var response = await _ticketService.GetTicketAsync(ticketNumber);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket");
            return StatusCode(500, new { message = "An error occurred while retrieving ticket" });
        }
    }

    /// <summary>
    /// Gets all tickets for a booking.
    /// </summary>
    [HttpGet("booking/{bookingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TicketResponse>>> GetBookingTicketsAsync(int bookingId)
    {
        try
        {
            _logger.LogInformation("Booking tickets requested: {BookingId}", bookingId);

            var response = await _ticketService.GetBookingTicketsAsync(bookingId);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking tickets");
            return StatusCode(500, new { message = "An error occurred while retrieving tickets" });
        }
    }

    /// <summary>
    /// Changes a ticket to a different flight.
    /// </summary>
    [HttpPut("{ticketNumber}/change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeTicketAsync(string ticketNumber, [FromBody] ChangeTicketDto dto)
    {
        try
        {
            _logger.LogInformation("Ticket change requested: {TicketNumber} to flight {NewFlightId}",
                ticketNumber, dto.NewFlightId);

            var success = await _ticketService.ChangeTicketAsync(ticketNumber, dto);
            return success ? Ok(new { message = "Ticket changed successfully" }) : BadRequest(new { message = "Ticket change failed" });
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
            _logger.LogError(ex, "Error changing ticket");
            return StatusCode(500, new { message = "An error occurred while changing ticket" });
        }
    }

    /// <summary>
    /// Downloads a ticket as PDF or HTML.
    /// </summary>
    [HttpGet("{ticketNumber}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadTicketAsync(string ticketNumber, [FromQuery] string format = "pdf")
    {
        try
        {
            _logger.LogInformation("Ticket download requested: {TicketNumber} as {Format}",
                ticketNumber, format);

            var fileBytes = await _ticketService.DownloadTicketAsync(ticketNumber, format);
            var contentType = format.ToLower() == "pdf" ? "application/pdf" : "text/html";
            var fileName = $"ticket_{ticketNumber}.{format}";

            return File(fileBytes, contentType, fileName);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading ticket");
            return StatusCode(500, new { message = "An error occurred while downloading ticket" });
        }
    }
}
