namespace API.Controllers;

using API.Application.Dtos.Payment;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentsController(
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        ILogger<PaymentsController> logger,
        IConfiguration configuration)
    {
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _logger = logger;
        _configuration = configuration;
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
            var userId = User.GetUserIdOrThrow();
            var isAdmin = User.IsInRole("Admin");

            _logger.LogInformation("Payment initiated for booking {BookingId} via {Provider}",
                dto.BookingId, dto.PaymentMethod);

            var response = await _paymentService.InitiatePaymentAsync(dto.BookingId, dto, userId, isAdmin);
            return Ok(response);
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
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
        API.Domain.Entities.Payment? matchedPayment = null;
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

            matchedPayment = (await _paymentRepository.GetAllAsync())
                .Where(p => !string.IsNullOrWhiteSpace(p.TransactionRef)
                    && string.Equals(p.TransactionRef.Trim(), txnRef, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Status == 0)
                .ThenByDescending(p => p.CreatedAt)
                .FirstOrDefault();

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
            var frontendReturnBaseUrl = _configuration["AppSettings:FrontendPaymentReturnUrl"]
                ?? "http://localhost:5173/payment-result";
            var redirectUrl = QueryHelpers.AddQueryString(frontendReturnBaseUrl, new Dictionary<string, string?>
            {
                ["provider"] = "vnpay",
                ["success"] = isSuccess ? "1" : "0",
                ["bookingId"] = matchedPayment?.BookingId.ToString(),
                ["paymentId"] = matchedPayment?.Id.ToString(),
                ["responseCode"] = responseCode,
                ["transactionStatus"] = transactionStatus,
                ["txnRef"] = txnRef,
                ["transactionNo"] = transactionNo,
                ["amount"] = amount,
                ["bankCode"] = bankCode,
                ["orderInfo"] = orderInfo
            });

            _logger.LogInformation("[VNPAY RESPONSE] Redirecting user to frontend: {RedirectUrl}", redirectUrl);
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling VNPAY return");
            var frontendReturnBaseUrl = _configuration["AppSettings:FrontendPaymentReturnUrl"]
                ?? "http://localhost:5173/payment-result";
            var fallbackUrl = QueryHelpers.AddQueryString(frontendReturnBaseUrl, new Dictionary<string, string?>
            {
                ["provider"] = "vnpay",
                ["success"] = "0",
                ["bookingId"] = matchedPayment?.BookingId.ToString(),
                ["paymentId"] = matchedPayment?.Id.ToString(),
                ["error"] = "callback_processing_failed"
            });
            return Redirect(fallbackUrl);
        }
    }
}


