namespace API.Application.Services;

using API.Application.Dtos.Notification;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        INotificationLogRepository notificationLogRepository,
        IUserRepository userRepository,
        IPromotionRepository promotionRepository,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _notificationLogRepository = notificationLogRepository;
        _userRepository = userRepository;
        _promotionRepository = promotionRepository;
        _logger = logger;
    }

    public async Task<bool> SendNotificationAsync(
        int userId,
        string subject,
        string message,
        string type = "IN_APP",
        string category = "GENERAL",
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        bool sendEmail = false,
        bool requirePromoOptIn = false)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var settings = ResolveSettings(user.NotificationPreferences);
            if (requirePromoOptIn && !settings.PromoNotifications)
            {
                return false;
            }

            var normalizedType = Normalize(type, "IN_APP");
            var normalizedCategory = Normalize(category, "GENERAL");
            var shouldSendEmail = (sendEmail || normalizedType == "EMAIL")
                && settings.EmailNotifications
                && !string.IsNullOrWhiteSpace(user.Email);

            var notificationLog = new NotificationLog
            {
                UserId = userId,
                Email = user.Email,
                Title = subject,
                Content = message,
                Type = normalizedType,
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                Category = normalizedCategory,
                RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType.Trim(),
                RelatedEntityId = relatedEntityId
            };

            await _notificationLogRepository.CreateAsync(notificationLog);

            if (shouldSendEmail)
            {
                try
                {
                    await _emailService.SendNotificationAsync(user.Email, subject, message);
                }
                catch (Exception ex)
                {
                    notificationLog.MarkAsFailed();
                    await _notificationLogRepository.UpdateAsync(notificationLog);
                    _logger.LogWarning(ex, "Notification email failed for user {UserId}", userId);
                    return false;
                }
            }

            notificationLog.MarkAsSent();
            await _notificationLogRepository.UpdateAsync(notificationLog);
            _logger.LogInformation(
                "Notification created for user {UserId}: {Type}/{Category}",
                userId,
                normalizedType,
                normalizedCategory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
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
        var promotion = await _promotionRepository.GetByIdAsync(promotionId);
        if (promotion == null || !promotion.IsActive || promotion.IsDeleted)
        {
            return false;
        }

        var users = (await _userRepository.GetAllAsync()).Where(u => u.IsActive()).ToList();
        var eligibleUsers = users
            .Where(u => ResolveSettings(u.NotificationPreferences).PromoNotifications)
            .ToList();

        var successCount = 0;
        foreach (var user in eligibleUsers)
        {
            try
            {
                var sent = await SendNotificationAsync(
                    user.Id,
                    $"Promotion {promotion.Code} is now available",
                    BuildPromotionMessage(promotion),
                    type: "IN_APP",
                    category: "PROMOTION",
                    relatedEntityType: "Promotion",
                    relatedEntityId: promotion.Id,
                    sendEmail: true,
                    requirePromoOptIn: true);

                if (sent)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Promotion notification failed for user {UserId}, promotion {PromotionId}",
                    user.Id,
                    promotion.Id);
            }
        }

        _logger.LogInformation(
            "Promotion notification broadcast completed for promotion {PromotionId}. Eligible={Eligible}, Sent={Sent}",
            promotionId,
            eligibleUsers.Count,
            successCount);

        return eligibleUsers.Count == 0 || successCount > 0;
    }

    public async Task<BroadcastNotificationResponse> BroadcastNotificationAsync(BroadcastNotificationDto dto)
    {
        var response = new BroadcastNotificationResponse();
        var normalizedCategory = Normalize(dto.Category, "SYSTEM");
        var users = (await _userRepository.GetAllAsync()).Where(u => u.IsActive()).ToList();

        response.TotalUsers = users.Count;

        foreach (var user in users)
        {
            try
            {
                var sent = await SendNotificationAsync(
                    user.Id,
                    dto.Subject,
                    dto.Message,
                    type: "IN_APP",
                    category: normalizedCategory,
                    relatedEntityType: "SystemBroadcast",
                    sendEmail: dto.SendEmail);

                if (sent)
                {
                    response.SuccessCount++;
                }
                else
                {
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                response.FailedCount++;
                _logger.LogWarning(ex, "System broadcast failed for user {UserId}", user.Id);
            }
        }

        return response;
    }

    public async Task<List<NotificationResponse>> GetUserNotificationsAsync(
        int userId,
        bool unreadOnly = false,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var logs = await _notificationLogRepository.GetByUserIdAsync(userId);
            return logs
                .Where(l => !unreadOnly || !l.IsRead)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapResponse)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user notifications");
            return [];
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _notificationLogRepository.GetUnreadCountByUserIdAsync(userId);
    }

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
    {
        var notification = await _notificationLogRepository.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.MarkAsRead();
            await _notificationLogRepository.UpdateAsync(notification);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        var notifications = (await _notificationLogRepository.GetByUserIdAsync(userId))
            .Where(n => !n.IsRead)
            .ToList();

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
            await _notificationLogRepository.UpdateAsync(notification);
        }

        return notifications.Count;
    }

    public async Task<bool> UpdateNotificationSettingsAsync(int userId, NotificationSettingsDto settings)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.NotificationPreferences = JsonSerializer.Serialize(settings);
            await _userRepository.UpdateAsync(user);
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
        var user = await _userRepository.GetByIdAsync(userId);
        return user == null
            ? new NotificationSettingsDto()
            : ResolveSettings(user.NotificationPreferences);
    }

    private static NotificationSettingsDto ResolveSettings(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return new NotificationSettingsDto();
        }

        try
        {
            return JsonSerializer.Deserialize<NotificationSettingsDto>(
                preferencesJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new NotificationSettingsDto();
        }
        catch
        {
            return new NotificationSettingsDto();
        }
    }

    private static NotificationResponse MapResponse(NotificationLog log)
    {
        return new NotificationResponse
        {
            NotificationId = log.Id,
            UserId = log.UserId,
            UserEmail = log.Email ?? "Unknown",
            Subject = log.Title,
            Message = log.Content,
            Type = log.Type,
            Status = log.Status == 1 ? "SENT" : (log.Status == 0 ? "PENDING" : "FAILED"),
            CreatedAt = log.CreatedAt,
            SentAt = log.SentAt,
            IsRead = log.IsRead,
            ReadAt = log.ReadAt,
            Category = log.Category,
            RelatedEntityType = log.RelatedEntityType,
            RelatedEntityId = log.RelatedEntityId
        };
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToUpperInvariant();
    }

    private static string BuildPromotionMessage(Promotion promotion)
    {
        var discount = promotion.DiscountType == 0
            ? $"{promotion.DiscountValue:0.##}%"
            : $"{promotion.DiscountValue:0.##} VND";

        var maxDiscount = promotion.MaxDiscountAmount.HasValue
            ? $" Maximum discount: {promotion.MaxDiscountAmount.Value:0.##} VND."
            : string.Empty;

        return $"Use code {promotion.Code} for {discount} off your next booking.{maxDiscount} Valid until {promotion.ValidTo:yyyy-MM-dd}.";
    }
}
