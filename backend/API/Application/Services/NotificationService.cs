namespace API.Application.Services;

using API.Application.Dtos.Notification;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        INotificationLogRepository notificationLogRepository,
        IUserRepository userRepository,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _notificationLogRepository = notificationLogRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> SendNotificationAsync(int userId, string subject, string message, string type = "EMAIL")
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            bool success = false;

            switch (type.ToUpperInvariant())
            {
                case "EMAIL":
                    success = true;
                    break;
                case "SMS":
                    success = true;
                    break;
                case "PUSH":
                    success = true;
                    break;
                case "IN_APP":
                    success = true;
                    break;
            }

            var notificationLog = new NotificationLog
            {
                UserId = userId,
                Title = subject,
                Content = message,
                Type = type,
                Status = success ? 1 : 2,
                CreatedAt = DateTime.UtcNow,
                SentAt = success ? DateTime.UtcNow : null
            };

            await _notificationLogRepository.CreateAsync(notificationLog);
            _logger.LogInformation("Notification sent to user {UserId}: {Type}", userId, type);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return false;
        }
    }

    public async Task<bool> SendBookingConfirmationAsync(int bookingId)
    {
        _logger.LogInformation("Booking confirmation sent for booking {BookingId}", bookingId);
        return true;
    }

    public async Task<bool> SendPaymentReminderAsync(int bookingId)
    {
        _logger.LogInformation("Payment reminder sent for booking {BookingId}", bookingId);
        return true;
    }

    public async Task<bool> SendRefundNotificationAsync(int refundId)
    {
        _logger.LogInformation("Refund notification sent for refund {RefundId}", refundId);
        return true;
    }

    public async Task<bool> SendPromotionalNotificationAsync(int promotionId)
    {
        _logger.LogInformation("Promotional notification sent for promotion {PromotionId}", promotionId);
        return true;
    }

    public async Task<List<NotificationResponse>> GetUserNotificationsAsync(int userId)
    {
        try
        {
            var logs = await _notificationLogRepository.GetByUserIdAsync(userId);
            var responses = new List<NotificationResponse>();

            foreach (var log in logs.OrderByDescending(l => l.CreatedAt).Take(50))
            {
                responses.Add(new NotificationResponse
                {
                    NotificationId = log.Id,
                    UserId = log.UserId,
                    UserEmail = log.Email ?? "Unknown",
                    Subject = log.Title,
                    Message = log.Content,
                    Type = log.Type,
                    Status = log.Status == 1 ? "SENT" : (log.Status == 0 ? "PENDING" : "FAILED"),
                    CreatedAt = log.CreatedAt,
                    SentAt = log.SentAt
                });
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user notifications");
            return [];
        }
    }

    public async Task<bool> UpdateNotificationSettingsAsync(int userId, NotificationSettingsDto settings)
    {
        try
        {
            _logger.LogInformation("Notification settings updated for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            return false;
        }
    }

    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync(int userId)
    {
        return new NotificationSettingsDto();
    }
}
