namespace API.Controllers;

using API.Application.Dtos.Payment;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using API.Extensions;
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
    /// Only the payment owner or admin can view payment details.
    /// </summary>
    [HttpGet("{paymentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetPaymentStatusAsync(int paymentId)
    {
        try
        {
            // Get current user ID from claims
            var userId = User.GetUserIdOrThrow();

            // Check if user is admin
            var isAdmin = User.IsInRole("Admin");

            var response = await _paymentService.GetPaymentStatusAsync(paymentId, userId, isAdmin);
            return Ok(response);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized payment status access attempt");
            return Unauthorized(new { message = ex.Message });
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
    /// Only the booking owner or admin can view payment history.
    /// </summary>
    [HttpGet("booking/{bookingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PaymentHistoryResponse>>> GetPaymentHistoryAsync(int bookingId)
    {
        try
        {
            // Get current user ID from claims
            var userId = User.GetUserIdOrThrow();

            // Check if user is admin
            var isAdmin = User.IsInRole("Admin");

            var response = await _paymentService.GetPaymentHistoryAsync(bookingId, userId, isAdmin);
            return Ok(response);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized payment history access attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history");
            return StatusCode(500, new { message = "An error occurred while retrieving payment history" });
        }
    }

    /// <summary>
    /// Handles payment callback from provider.
    /// SECURITY: Verifies callback signature and amount before confirming payment.
    /// Must include: TransactionId, Amount, Status, Signature, RawData
    /// </summary>
    [HttpPost("{paymentId}/callback")]
    [AllowAnonymous] // Must be anonymous to receive callbacks from payment providers
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPaymentCallbackAsync(int paymentId, [FromBody] PaymentCallbackDto callback)
    {
        try
        {
            _logger.LogInformation(
                "Payment callback received for payment {PaymentId}, TransactionId: {TransactionId}, Amount: {Amount}",
                paymentId, callback.TransactionId, callback.Amount);

            // Log the callback for audit trail (security)
            _logger.LogInformation("Callback details - Status: {Status}, Signature present: {HasSignature}",
                callback.Status, !string.IsNullOrEmpty(callback.Signature));

            // ProcessPaymentAsync internally validates callback (signature, amount, transaction ID)
            var success = await _paymentService.ProcessPaymentAsync(paymentId, callback);

            if (!success)
            {
                _logger.LogWarning("Payment callback processing failed for payment {PaymentId}", paymentId);
                return BadRequest(new { message = "Payment processing failed" });
            }

            // Return 200 OK to acknowledge callback receipt (required by payment providers)
            // But payment confirmation only happens AFTER validation
            return Ok(new { message = "Payment processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback for payment {PaymentId}", paymentId);
            // Still return 200 to prevent provider retry, but don't confirm payment
            return Ok(new { message = "Callback received (validation pending)" });
        }
    }
}
