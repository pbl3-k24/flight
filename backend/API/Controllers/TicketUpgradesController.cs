namespace API.Controllers;

using API.Application.Dtos.TicketUpgrade;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TicketUpgradesController : ControllerBase
{
    private readonly ITicketUpgradeService _ticketUpgradeService;
    private readonly ILogger<TicketUpgradesController> _logger;

    public TicketUpgradesController(
        ITicketUpgradeService ticketUpgradeService,
        ILogger<TicketUpgradesController> logger)
    {
        _ticketUpgradeService = ticketUpgradeService;
        _logger = logger;
    }

    [HttpPost("bookings/{bookingId}/tickets/{ticketId}/upgrade/quote")]
    public async Task<ActionResult<TicketUpgradeQuoteResponseDto>> QuoteAsync(
        int bookingId,
        int ticketId,
        [FromBody] TicketUpgradeQuoteRequestDto dto)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            var quote = await _ticketUpgradeService.GetQuoteAsync(bookingId, ticketId, dto.ToSeatClassId, userId);
            return Ok(quote);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error quoting ticket upgrade");
            return StatusCode(500, new { message = "An error occurred while quoting ticket upgrade" });
        }
    }

    [HttpPost("bookings/{bookingId}/tickets/{ticketId}/upgrade-requests")]
    public async Task<ActionResult<TicketUpgradeRequestResponseDto>> CreateRequestAsync(
        int bookingId,
        int ticketId,
        [FromBody] CreateTicketUpgradeRequestDto dto)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            var request = await _ticketUpgradeService.CreateRequestAsync(bookingId, ticketId, dto.ToSeatClassId, userId);
            return Ok(request);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating upgrade request");
            return StatusCode(500, new { message = "An error occurred while creating upgrade request" });
        }
    }

    [HttpPost("ticket-upgrades/{requestId}/payments")]
    public async Task<ActionResult<TicketUpgradePaymentResponseDto>> InitiatePaymentAsync(
        int requestId,
        [FromBody] InitiateTicketUpgradePaymentDto dto)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            var response = await _ticketUpgradeService.InitiatePaymentAsync(requestId, dto.PaymentMethod, userId);
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
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating upgrade payment");
            return StatusCode(500, new { message = "An error occurred while initiating upgrade payment" });
        }
    }
}
