namespace API.Infrastructure.ExternalServices;

using API.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class MomoPaymentProvider : IPaymentProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<MomoPaymentProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MomoPaymentProvider(
        IConfiguration config,
        ILogger<MomoPaymentProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PaymentProviderResponse> GeneratePaymentLinkAsync(PaymentProviderRequest request)
    {
        try
        {
            var partnerCode = _config["Payment:Momo:PartnerCode"];
            var secretKey = _config["Payment:Momo:SecretKey"];
            var apiUrl = _config["Payment:Momo:ApiUrl"];
            var notifyUrl = _config["Payment:Momo:NotifyUrl"];

            var requestId = Guid.NewGuid().ToString();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var requestData = new
            {
                partnerCode,
                requestId,
                amount = (long)request.Amount,
                orderId = $"BOOKING{request.BookingId}{timestamp}",
                orderInfo = request.OrderDescription ?? $"Flight booking #{request.BookingId}",
                returnUrl = request.ReturnUrl,
                notifyUrl,
                requestType = "captureMoMoWallet",
                signature = ""
            };

            // Generate signature
            var signatureData = $"accessKey=&amount={(long)request.Amount}&extraData=&orderId={requestData.orderId}&orderInfo={requestData.orderInfo}&partnerCode={partnerCode}&requestId={requestId}&requestType=captureMoMoWallet";
            var signature = SignData(signatureData, secretKey);

            // Create request payload
            var payload = new
            {
                partnerCode,
                requestId,
                amount = (long)request.Amount,
                orderId = requestData.orderId,
                orderInfo = requestData.orderInfo,
                returnUrl = request.ReturnUrl,
                notifyUrl,
                requestType = "captureMoMoWallet",
                signature
            };

            using var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync(apiUrl ?? "https://test-payment.momo.vn/gw_payment/payment", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Failed to generate Momo payment link");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var momoResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var paymentLink = momoResponse.GetProperty("payUrl").GetString() ?? "";
            var qrCode = momoResponse.GetProperty("qrCodeUrl").GetString();

            _logger.LogInformation("Momo payment link generated: {RequestId}", requestId);

            return new PaymentProviderResponse
            {
                PaymentLink = paymentLink,
                QrCode = qrCode,
                TransactionId = requestId,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Momo payment link");
            throw;
        }
    }

    public async Task<bool> VerifyCallbackSignatureAsync(string signature, string data)
    {
        try
        {
            var secretKey = _config["Payment:Momo:SecretKey"];
            var expectedSignature = SignData(data, secretKey);
            
            return signature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying Momo callback signature");
            return false;
        }
    }

    public async Task<string> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            // Would query Momo API for status
            // For now, returning pending
            return "pending";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Momo payment status");
            return "unknown";
        }
    }

    public async Task<bool> RefundAsync(string transactionId, decimal amount)
    {
        try
        {
            // Implement Momo refund API call
            _logger.LogInformation("Refund requested for Momo transaction: {TransactionId}", transactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding Momo payment");
            return false;
        }
    }

    private string SignData(string data, string secretKey)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
