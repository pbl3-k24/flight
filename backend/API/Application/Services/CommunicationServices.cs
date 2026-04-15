namespace API.Application.Services;

using API.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class SmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SmsService(
        IConfiguration config,
        ILogger<SmsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            // Example: Twilio or AWS SNS integration
            // For now, just log
            _logger.LogInformation("SMS sent to {PhoneNumber}: {Message}", phoneNumber, message.Substring(0, Math.Min(50, message.Length)));
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS");
            return false;
        }
    }

    public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
    {
        try
        {
            var message = $"Your OTP is: {otp}. Do not share with anyone.";
            return await SendSmsAsync(phoneNumber, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP");
            return false;
        }
    }
}

public class PushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public PushNotificationService(
        IConfiguration config,
        ILogger<PushNotificationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> SendPushAsync(int userId, string title, string body)
    {
        try
        {
            // Example: Firebase Cloud Messaging integration
            // For now, just log
            _logger.LogInformation("Push notification sent to user {UserId}: {Title}", userId, title);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification");
            return false;
        }
    }

    public async Task<bool> SendBulkPushAsync(List<int> userIds, string title, string body)
    {
        try
        {
            var tasks = userIds.Select(userId => SendPushAsync(userId, title, body));
            var results = await Task.WhenAll(tasks);
            
            _logger.LogInformation("Bulk push sent to {Count} users, {Success} succeeded", 
                userIds.Count, results.Count(r => r));
            
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk push notifications");
            return false;
        }
    }
}
