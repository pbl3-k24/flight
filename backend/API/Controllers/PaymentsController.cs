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

    /// <summary>
    /// Nhận redirect từ VNPAY sau khi thanh toán.
    /// VNPAY sẽ gửi kết quả về đây qua query params (GET).
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayReturnAsync()
    {
        try
        {
            var queryParams = Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            // Log toàn bộ params VNPAY trả về
            _logger.LogInformation("[VNPAY RESPONSE] Received {Count} params from VNPAY redirect:", queryParams.Count);
            foreach (var (key, value) in queryParams.OrderBy(x => x.Key))
            {
                _logger.LogInformation("  {Key} = {Value}", key, value);
            }

            var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "N/A");
            var transactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", "N/A");
            var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef", "N/A");
            var amount = queryParams.GetValueOrDefault("vnp_Amount", "N/A");
            var orderInfo = queryParams.GetValueOrDefault("vnp_OrderInfo", "N/A");
            var bankCode = queryParams.GetValueOrDefault("vnp_BankCode", "N/A");
            var payDate = queryParams.GetValueOrDefault("vnp_PayDate", "N/A");
            var transactionNo = queryParams.GetValueOrDefault("vnp_TransactionNo", "N/A");
            var secureHash = queryParams.GetValueOrDefault("vnp_SecureHash", "");

            // vnp_ResponseCode = "00" là thành công
            var isSuccess = responseCode == "00";

            _logger.LogInformation(
                "[VNPAY RESPONSE] TxnRef={TxnRef} | ResponseCode={ResponseCode} | Success={IsSuccess}",
                txnRef, responseCode, isSuccess);

            // Xử lý payment callback nếu thành công
            if (isSuccess && !string.IsNullOrEmpty(txnRef))
            {
                try
                {
                    var amountDecimal = long.TryParse(amount, out var amountLong) 
                        ? amountLong / 100m  // VNPAY trả về amount * 100
                        : 0m;

                    // Build raw data for signature verification (exclude vnp_SecureHash and vnp_SecureHashType)
                    // Must URL Encode both keys and values as per VNPAY 2.1.0 standard
                    var rawDataBuilder = new System.Text.StringBuilder();
                    foreach (var p in queryParams
                        .Where(p => p.Key != "vnp_SecureHash" && p.Key != "vnp_SecureHashType" && !string.IsNullOrEmpty(p.Value))
                        .OrderBy(p => p.Key, StringComparer.Ordinal))
                    {
                        if (rawDataBuilder.Length > 0)
                        {
                            rawDataBuilder.Append("&");
                        }
                        rawDataBuilder.Append(System.Net.WebUtility.UrlEncode(p.Key) + "=" + System.Net.WebUtility.UrlEncode(p.Value));
                    }
                    var rawData = rawDataBuilder.ToString();

                    var callback = new PaymentCallbackDto
                    {
                        TransactionId = txnRef,
                        Status = isSuccess ? "success" : "failed",
                        Amount = amountDecimal,
                        Signature = secureHash,
                        RawData = rawData,
                        AdditionalData = queryParams
                    };

                    await _paymentService.ProcessVnpayCallbackAsync(callback);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing VNPAY callback for TxnRef {TxnRef}", txnRef);
                }
            }

            // Trả về HTML đơn giản để xem kết quả ngay trên browser
            var cssClass = isSuccess ? "success" : "fail";
            var statusText = isSuccess ? "✅ Thanh toán thành công" : "❌ Thanh toán thất bại / bị hủy";
            var amountDisplay = long.TryParse(amount, out var amt) ? (amt / 100).ToString("N0") + " VND" : amount;
            var allParams = string.Join("\n", queryParams.OrderBy(x => x.Key).Select(x => $"{x.Key} = {x.Value}"));

            var html = $$"""
                <!DOCTYPE html>
                <html lang="vi">
                <head><meta charset="utf-8"><title>VNPAY Ket qua</title>
                <style>
                  body { font-family: sans-serif; max-width: 600px; margin: 40px auto; padding: 20px; }
                  .success { color: green; } .fail { color: red; }
                  table { width: 100%; border-collapse: collapse; }
                  td, th { border: 1px solid #ccc; padding: 8px; text-align: left; }
                  th { background: #f0f0f0; }
                </style></head>
                <body>
                  <h2 class="{{cssClass}}">{{statusText}}</h2>
                  <table>
                    <tr><th>Thong tin</th><th>Gia tri</th></tr>
                    <tr><td>Ma giao dich (TxnRef)</td><td>{{txnRef}}</td></tr>
                    <tr><td>So GD VNPAY</td><td>{{transactionNo}}</td></tr>
                    <tr><td>So tien</td><td>{{amountDisplay}}</td></tr>
                    <tr><td>Ngan hang</td><td>{{bankCode}}</td></tr>
                    <tr><td>Noi dung</td><td>{{orderInfo}}</td></tr>
                    <tr><td>Ngay thanh toan</td><td>{{payDate}}</td></tr>
                    <tr><td>Response Code</td><td>{{responseCode}}</td></tr>
                    <tr><td>Transaction Status</td><td>{{transactionStatus}}</td></tr>
                  </table>
                  <br/>
                  <details><summary>Toan bo params nhan duoc</summary>
                  <pre>{{allParams}}</pre>
                  </details>
                </body></html>
                """;

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling VNPAY return");
            return Content("<html><body><h2>Error processing payment result</h2></body></html>", "text/html");
        }
    }
}
