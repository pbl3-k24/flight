namespace API.Controllers;

using API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/test/vnpay")]
[AllowAnonymous]
public class TestVnpayController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestVnpayController> _logger;

    public TestVnpayController(IConfiguration configuration, ILogger<TestVnpayController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("test-url")]
    public IActionResult TestVnpayUrl()
    {
        try
        {
            var vnpaySettings = _configuration.GetSection("VNPAY");
            var baseUrl = vnpaySettings["BaseUrl"];
            var tmnCode = vnpaySettings["TmnCode"];
            var hashSecret = vnpaySettings["HashSecret"];
            var returnUrl = vnpaySettings["ReturnUrl"];

            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var amount = 10000000L; // 100,000 VND * 100
            var txnRef = $"TEST{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var orderInfo = "Test thanh toan";

            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_Command"] = "pay",
                ["vnp_CreateDate"] = createDate,
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = "127.0.0.1",
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_TxnRef"] = txnRef,
                ["vnp_Version"] = "2.1.0"
            };

            // Hash data
            var hashData = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            var secureHash = CreateHmacSha512(hashSecret, hashData);

            // Build URL
            var queryString = string.Join("&", parameters.Select(p => 
                $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var paymentUrl = $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";

            var result = new
            {
                config = new
                {
                    baseUrl,
                    tmnCode,
                    hashSecretLength = hashSecret?.Length ?? 0,
                    returnUrl
                },
                parameters = parameters.ToDictionary(p => p.Key, p => p.Value),
                hashData,
                secureHash,
                paymentUrl,
                urlLength = paymentUrl.Length
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing VNPAY URL");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    private static string CreateHmacSha512(string secretKey, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
