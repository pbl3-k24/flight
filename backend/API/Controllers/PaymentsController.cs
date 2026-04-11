using FlightBooking.Application.DTOs.Payment;
using FlightBooking.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController(IPaymentService paymentService, IPaymentWebhookService webhookService, IRefundService refundService) : ControllerBase
{
    [HttpPost("{bookingId}/initiate")]
    public async Task<IActionResult> InitiatePayment(Guid bookingId, [FromBody] InitiatePaymentRequest request)
    {
        var result = await paymentService.InitiatePaymentAsync(bookingId, request.Gateway, request.ReturnUrl);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var payment = await paymentService.GetByIdAsync(id);
        return Ok(payment);
    }

    [AllowAnonymous]
    [HttpPost("webhook/{gateway}")]
    public async Task<IActionResult> Webhook(string gateway)
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync();
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        await webhookService.HandleWebhookAsync(gateway, rawPayload, headers);
        return Ok();
    }

    [HttpPost("refund")]
    public async Task<IActionResult> RequestRefund([FromBody] RefundRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var refund = await refundService.RequestRefundAsync(request, userId);
        return Ok(refund);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("refunds/pending")]
    public async Task<IActionResult> GetPendingRefunds([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var refunds = await refundService.GetPendingAsync();
        return Ok(refunds);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("refunds/{id}/approve")]
    public async Task<IActionResult> ApproveRefund(Guid id)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var refund = await refundService.ApproveRefundAsync(id, adminId);
        return Ok(refund);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("refunds/{id}/reject")]
    public async Task<IActionResult> RejectRefund(Guid id, [FromBody] string reason)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var refund = await refundService.RejectRefundAsync(id, reason, adminId);
        return Ok(refund);
    }
}

public record InitiatePaymentRequest(string Gateway, string ReturnUrl);
