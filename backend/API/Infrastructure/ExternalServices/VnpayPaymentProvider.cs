namespace API.Infrastructure.ExternalServices;

using API.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class VnpayRefundRequest
{
    public string TxnRef { get; set; } = null!;
    public string TransactionDate { get; set; } = null!;
    public decimal Amount { get; set; }
    public string OrderInfo { get; set; } = null!;
    public string CreateBy { get; set; } = null!;
}

public class VnpayRefundResponse
{
    public bool Success { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TransactionNo { get; set; }
}

public class VnpayPaymentProvider : IPaymentProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VnpayPaymentProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public VnpayPaymentProvider(
        IConfiguration configuration,
        ILogger<VnpayPaymentProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public Task<PaymentProviderResponse> GeneratePaymentLinkAsync(PaymentProviderRequest request)
    {
        try
        {
            var vnpaySettings = _configuration.GetSection("VNPAY");
            var baseUrl = vnpaySettings["BaseUrl"] ?? throw new InvalidOperationException("VNPAY:BaseUrl is not configured");
            var tmnCode = vnpaySettings["TmnCode"] ?? throw new InvalidOperationException("VNPAY:TmnCode is not configured");
            var hashSecret = vnpaySettings["HashSecret"] ?? throw new InvalidOperationException("VNPAY:HashSecret is not configured");
            var version = vnpaySettings["Version"] ?? "2.1.0";
            var command = vnpaySettings["Command"] ?? "pay";
            var currCode = vnpaySettings["CurrCode"] ?? "VND";
            var locale = vnpaySettings["Locale"] ?? "vn";
            var returnUrl = request.ReturnUrl ?? vnpaySettings["ReturnUrl"]
                ?? throw new InvalidOperationException("VNPAY:ReturnUrl is not configured");

            var createDate = GetVietnamNow().ToString("yyyyMMddHHmmss");
            var amount = (long)(request.Amount * 100m);
            
            // TxnRef phải là số hoặc chữ, không quá 100 ký tự
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var transactionRef = $"{request.BookingId}{timestamp}";
            
            // VNPAY chỉ chấp nhận chữ, số, khoảng trắng, dấu gạch ngang trong OrderInfo
            var orderInfo = string.IsNullOrWhiteSpace(request.OrderDescription)
                ? $"Thanhtoandatve{request.BookingId}"
                : System.Text.RegularExpressions.Regex.Replace(request.OrderDescription, @"[^a-zA-Z0-9\s\-]", "").Trim();
            
            // Giới hạn độ dài OrderInfo (max 255 chars theo VNPay)
            if (orderInfo.Length > 255)
            {
                orderInfo = orderInfo.Substring(0, 255);
            }

            // Tạo parameters đã sorted - QUAN TRỌNG: phải đúng thứ tự alphabet
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_Command"] = command,
                ["vnp_CreateDate"] = createDate,
                ["vnp_CurrCode"] = currCode,
                ["vnp_IpAddr"] = "113.161.84.26", // IP public giả lập (VNPay sandbox không chấp nhận 127.0.0.1)
                ["vnp_Locale"] = locale,
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_TxnRef"] = transactionRef,
                ["vnp_Version"] = version
            };

            // Build hash data and query string (must use WebUtility.UrlEncode)
            var hashDataBuilder = new StringBuilder();
            var queryStringBuilder = new StringBuilder();

            foreach (var kv in parameters)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    if (hashDataBuilder.Length > 0)
                    {
                        hashDataBuilder.Append("&");
                        queryStringBuilder.Append("&");
                    }
                    hashDataBuilder.Append(System.Net.WebUtility.UrlEncode(kv.Key) + "=" + System.Net.WebUtility.UrlEncode(kv.Value));
                    queryStringBuilder.Append(System.Net.WebUtility.UrlEncode(kv.Key) + "=" + System.Net.WebUtility.UrlEncode(kv.Value));
                }
            }

            var hashData = hashDataBuilder.ToString();
            var secureHash = CreateHmacSha512(hashSecret, hashData);

            var paymentUrl = $"{baseUrl}?{queryStringBuilder.ToString()}&vnp_SecureHash={secureHash}";

            _logger.LogInformation("[VNPAY] Payment URL generated for booking {BookingId}", request.BookingId);
            _logger.LogInformation("[VNPAY] TmnCode: {TmnCode}", tmnCode);
            _logger.LogInformation("[VNPAY] Amount: {Amount} VND (x100 = {AmountParam})", request.Amount, amount);
            _logger.LogInformation("[VNPAY] TxnRef: {TxnRef}", transactionRef);
            _logger.LogInformation("[VNPAY] OrderInfo: {OrderInfo}", orderInfo);
            _logger.LogInformation("[VNPAY] CreateDate: {CreateDate}", createDate);
            _logger.LogInformation("[VNPAY] Hash data: {HashData}", hashData);
            _logger.LogInformation("[VNPAY] Secure hash: {SecureHash}", secureHash);
            _logger.LogInformation("[VNPAY] Full URL length: {Length} chars", paymentUrl.Length);

            var qrCodeBase64 = GenerateQrCodeBase64(paymentUrl);

            return Task.FromResult(new PaymentProviderResponse
            {
                PaymentLink = paymentUrl,
                QrCode = qrCodeBase64,
                TransactionId = transactionRef,
                ExpiresAt = GetVietnamNow().AddMinutes(15)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating VNPAY payment link for booking {BookingId}", request.BookingId);
            throw;
        }
    }

    public Task<bool> VerifyCallbackSignatureAsync(string signature, string data)
    {
        var hashSecret = _configuration["VNPAY:HashSecret"];
        if (string.IsNullOrWhiteSpace(hashSecret))
        {
            return Task.FromResult(false);
        }

        var expectedSignature = CreateHmacSha512(hashSecret, data);
        return Task.FromResult(FixedTimeEquals(signature, expectedSignature));
    }

    public Task<string> GetPaymentStatusAsync(string transactionId)
    {
        return Task.FromResult("pending");
    }

    public Task<bool> RefundAsync(string transactionId, decimal amount)
    {
        return Task.FromResult(false);
    }

    public async Task<VnpayRefundResponse> ProcessRefundAsync(VnpayRefundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _configuration.GetSection("VNPAY");
            
            // Check if refund is enabled (sandbox may not support refund API)
            var enableRefund = settings.GetValue<bool>("EnableRefund", false);
            if (!enableRefund)
            {
                _logger.LogWarning("[VNPAY REFUND] Refund is disabled in configuration (sandbox limitation)");
                return new VnpayRefundResponse
                {
                    Success = false,
                    ResponseCode = "DISABLED",
                    Message = "VNPay refund is disabled in sandbox environment. Enable in production with IP whitelist."
                };
            }
            
            var tmnCode = settings["TmnCode"] ?? throw new InvalidOperationException("VNPAY:TmnCode is not configured");
            var hashSecret = settings["HashSecret"] ?? throw new InvalidOperationException("VNPAY:HashSecret is not configured");
            var apiUrl = settings["RefundUrl"] ?? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
            var version = settings["Version"] ?? "2.1.0";
            var createDate = GetVietnamNow().ToString("yyyyMMddHHmmss");
            var requestId = Guid.NewGuid().ToString("N");

            _logger.LogInformation("[VNPAY REFUND] Starting refund for TxnRef: {TxnRef}, Amount: {Amount}", 
                request.TxnRef, request.Amount);

            // VNPAY yêu cầu vnp_Amount = số tiền VND × 100
            var refundAmount = (long)(request.Amount * 100m);
            
            // Format vnp_CreateBy: Tên định danh cụ thể (không có ký tự đặc biệt)
            var createBy = string.IsNullOrWhiteSpace(request.CreateBy) 
                ? "SYSTEM" 
                : System.Text.RegularExpressions.Regex.Replace(request.CreateBy, @"[^a-zA-Z0-9]", "").ToUpperInvariant();
            
            // QUAN TRỌNG: Thứ tự parameters cho Refund API khác với Payment API
            // Phải theo đúng thứ tự alphabet của VNPay Transaction API
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Amount"] = refundAmount.ToString(),
                ["vnp_Command"] = "refund",
                ["vnp_CreateBy"] = createBy,
                ["vnp_CreateDate"] = createDate,
                ["vnp_IpAddr"] = "113.161.84.26",
                ["vnp_OrderInfo"] = request.OrderInfo,
                ["vnp_RequestId"] = requestId,
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_TransactionDate"] = request.TransactionDate,
                ["vnp_TransactionType"] = "02",  // 02 = Hoàn trả toàn phần
                ["vnp_TxnRef"] = request.TxnRef,
                ["vnp_Version"] = version
            };

            // Build hash data cho Refund API (thứ tự đã sorted)
            var hashData = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            var secureHash = CreateHmacSha512(hashSecret, hashData);
            
            // Thêm SecureHash vào parameters (KHÔNG tham gia vào hash data)
            parameters["vnp_SecureHash"] = secureHash;

            _logger.LogInformation("[VNPAY REFUND] Request ID: {RequestId}", requestId);
            _logger.LogInformation("[VNPAY REFUND] CreateBy: {CreateBy}", createBy);
            _logger.LogInformation("[VNPAY REFUND] TransactionDate: {TransactionDate}", request.TransactionDate);
            _logger.LogInformation("[VNPAY REFUND] Hash data: {HashData}", hashData);
            _logger.LogInformation("[VNPAY REFUND] Secure hash: {SecureHash}", secureHash);
            _logger.LogInformation("[VNPAY REFUND] API URL: {ApiUrl}", apiUrl);

            using var client = _httpClientFactory.CreateClient();
            
            // VNPay Refund API yêu cầu JSON format
            var jsonPayload = JsonSerializer.Serialize(parameters);
            _logger.LogInformation("[VNPAY REFUND] Request payload: {Payload}", jsonPayload);
            
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            // QUAN TRỌNG: VNPay có thể yêu cầu IP whitelist cho Refund API
            // Nếu gặp 403, cần đăng ký IP server với VNPay
            using var httpResponse = await client.PostAsync(apiUrl, content, cancellationToken);
            var responseText = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("[VNPAY REFUND] Response status: {StatusCode}", httpResponse.StatusCode);
            _logger.LogInformation("[VNPAY REFUND] Response body: {Response}", responseText);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("[VNPAY REFUND] HTTP error {StatusCode}: {Response}", 
                    httpResponse.StatusCode, responseText);
                
                // Nếu 403 Forbidden - IP chưa được whitelist
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("[VNPAY REFUND] 403 Forbidden - Possible causes:");
                    _logger.LogWarning("  1. Server IP not whitelisted with VNPay");
                    _logger.LogWarning("  2. Invalid vnp_CreateBy format");
                    _logger.LogWarning("  3. Incorrect hash algorithm or parameter order");
                    _logger.LogWarning("  Current IP: 113.161.84.26 (mock IP for sandbox)");
                }
                    
                return new VnpayRefundResponse
                {
                    Success = false,
                    ResponseCode = httpResponse.StatusCode.ToString(),
                    Message = $"HTTP error from VNPAY refund API: {responseText}"
                };
            }

            var payload = JsonSerializer.Deserialize<JsonElement>(responseText);
            var responseCode = payload.TryGetProperty("vnp_ResponseCode", out var code)
                ? code.GetString() ?? string.Empty
                : string.Empty;

            var message = payload.TryGetProperty("vnp_Message", out var msg)
                ? msg.GetString() ?? string.Empty
                : string.Empty;

            var transactionNo = payload.TryGetProperty("vnp_TransactionNo", out var txnNo)
                ? txnNo.GetString()
                : null;

            _logger.LogInformation("[VNPAY REFUND] Response code: {ResponseCode}, Message: {Message}", 
                responseCode, message);

            return new VnpayRefundResponse
            {
                Success = string.Equals(responseCode, "00", StringComparison.OrdinalIgnoreCase),
                ResponseCode = responseCode,
                Message = message,
                TransactionNo = transactionNo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VNPAY REFUND] Error calling VNPAY refund API for TxnRef {TxnRef}", request.TxnRef);
            return new VnpayRefundResponse
            {
                Success = false,
                ResponseCode = "EX",
                Message = ex.Message
            };
        }
    }

    private static string CreateHmacSha512(string secretKey, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Sinh ảnh QR code dạng base64 PNG từ URL thanh toán VNPAY.
    /// FE dùng: <img src="data:image/png;base64,{result}" />
    /// </summary>
    private static string GenerateQrCodeBase64(string paymentUrl)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(paymentUrl, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(5); // 5px per module
        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }

    private static bool FixedTimeEquals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left.Trim());
        var rightBytes = Encoding.UTF8.GetBytes(right.Trim());

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static DateTime GetVietnamNow()
    {
        try
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }
        catch
        {
            return DateTime.UtcNow.AddHours(7);
        }
    }
}