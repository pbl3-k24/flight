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
            var partnerCode = ResolveConfig("Payment:Momo:PartnerCode", "MomoAPI:PartnerCode");
            var secretKey = ResolveConfig("Payment:Momo:SecretKey", "MomoAPI:SecretKey");
            var accessKey = ResolveConfig("Payment:Momo:AccessKey", "MomoAPI:AccessKey");
            var apiUrl = ResolveConfig("Payment:Momo:ApiUrl", "MomoAPI:MomoApiUrl")
                ?? "https://test-payment.momo.vn/v2/gateway/api/create";
            var notifyUrl = ResolveConfig("Payment:Momo:NotifyUrl", "MomoAPI:NotifyUrl")
                ?? throw new InvalidOperationException("Momo notify URL is not configured");
            var returnUrl = request.ReturnUrl
                ?? ResolveConfig("Payment:Momo:ReturnUrl", "MomoAPI:ReturnUrl")
                ?? throw new InvalidOperationException("Momo return URL is not configured");
            var requestType = ResolveConfig("Payment:Momo:RequestType", "MomoAPI:RequestType")
                ?? "captureMoMoWallet";

            if (string.IsNullOrWhiteSpace(partnerCode)
                || string.IsNullOrWhiteSpace(secretKey)
                || string.IsNullOrWhiteSpace(accessKey))
            {
                throw new InvalidOperationException("Momo credentials are not configured correctly");
            }

            var requestId = Guid.NewGuid().ToString();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var orderId = $"BOOKING{request.BookingId}{timestamp}";
            var orderInfo = request.OrderDescription ?? $"Flight booking #{request.BookingId}";

            var amount = Convert.ToInt64(decimal.Round(request.Amount, 0, MidpointRounding.AwayFromZero));

            // Generate signature
            var signatureData = $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
            var signature = SignData(signatureData, secretKey);

            // Create request payload
            var payload = new
            {
                partnerCode,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                requestType,
                extraData = string.Empty,
                lang = "vi",
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

            var paymentLink = momoResponse.TryGetProperty("payUrl", out var payUrlElement)
                ? payUrlElement.GetString() ?? string.Empty
                : string.Empty;

            var deeplink = momoResponse.TryGetProperty("deeplink", out var deeplinkElement)
                ? deeplinkElement.GetString()
                : null;

            var qrCode = momoResponse.TryGetProperty("qrCodeUrl", out var qrCodeElement)
                ? qrCodeElement.GetString()
                : paymentLink;

            if (string.IsNullOrWhiteSpace(paymentLink))
            {
                throw new InvalidOperationException($"Momo response missing payUrl. Response: {responseContent}");
            }

            _logger.LogInformation("Momo payment link generated: {RequestId}", requestId);

            return new PaymentProviderResponse
            {
                PaymentLink = paymentLink,
                QrCode = string.IsNullOrWhiteSpace(qrCode) ? paymentLink : qrCode,
                TransactionId = orderId,
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
            var secretKey = ResolveConfig("Payment:Momo:SecretKey", "MomoAPI:SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey)
                || string.IsNullOrWhiteSpace(signature)
                || string.IsNullOrWhiteSpace(data))
            {
                return false;
            }

            var expectedSignature = SignData(data, secretKey);

            return FixedTimeEquals(signature, expectedSignature);
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

    private string? ResolveConfig(string primaryKey, string fallbackKey)
    {
        var primary = _config[primaryKey];
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return _config[fallbackKey];
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left.Trim());
        var rightBytes = Encoding.UTF8.GetBytes(right.Trim());

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
