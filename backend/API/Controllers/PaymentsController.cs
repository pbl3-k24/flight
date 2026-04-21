namespace API.Controllers;

using API.Application.Dtos.Payment;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a payment for a booking.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResponse>> InitiatePaymentAsync([FromBody] InitiatePaymentDto dto)
    {
        try
        {
            _logger.LogInformation("Payment initiated for booking {BookingId} via {Provider}",
                dto.BookingId, dto.PaymentMethod);

            var response = await _paymentService.InitiatePaymentAsync(dto.BookingId, dto);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return StatusCode(500, new { message = "An error occurred while initiating payment" });
        }
    }

    /// <summary>
    /// Gets payment status.
    /// </summary>
    [HttpGet("{paymentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetPaymentStatusAsync(int paymentId)
    {
        try
        {
            var response = await _paymentService.GetPaymentStatusAsync(paymentId);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status");
            return StatusCode(500, new { message = "An error occurred while retrieving payment status" });
        }
    }

    /// <summary>
    /// Gets payment history for a booking.
    /// </summary>
    [HttpGet("booking/{bookingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentHistoryResponse>>> GetPaymentHistoryAsync(int bookingId)
    {
        try
        {
            var response = await _paymentService.GetPaymentHistoryAsync(bookingId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history");
            return StatusCode(500, new { message = "An error occurred while retrieving payment history" });
        }
    }

    /// <summary>
    /// Handles payment callback from provider.
    /// </summary>
    [HttpPost("{paymentId}/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPaymentCallbackAsync(int paymentId, [FromBody] PaymentCallbackDto callback)
    {
        try
        {
            _logger.LogInformation("Payment callback received for payment {PaymentId}", paymentId);

            var success = await _paymentService.ProcessPaymentAsync(paymentId, callback);
            return success ? Ok(new { message = "Payment processed successfully" }) : BadRequest(new { message = "Payment processing failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback");
            return StatusCode(500, new { message = "An error occurred while processing payment" });
        }
    }
}
