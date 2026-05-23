namespace API.Application.Interfaces;

using API.Application.Dtos.Notification;

public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a user.
    /// </summary>
    Task<bool> SendNotificationAsync(
        int userId,
        string subject,
        string message,
        string type = "IN_APP",
        string category = "GENERAL",
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        bool sendEmail = false,
        bool requirePromoOptIn = false);

    /// <summary>
    /// Sends booking confirmation notification.
    /// </summary>
    Task<bool> SendBookingConfirmationAsync(int bookingId);

    /// <summary>
    /// Sends payment reminder notification.
    /// </summary>
    Task<bool> SendPaymentReminderAsync(int bookingId);

    /// <summary>
    /// Sends refund processed notification.
    /// </summary>
    Task<bool> SendRefundNotificationAsync(int refundId);

    /// <summary>
    /// Sends promotional notification.
    /// </summary>
    Task<bool> SendPromotionalNotificationAsync(int promotionId);

    /// <summary>
    /// Sends a broadcast notification to all active users (Admin).
    /// </summary>
    Task<BroadcastNotificationResponse> BroadcastNotificationAsync(BroadcastNotificationDto dto);

    /// <summary>
    /// Gets notification history for user.
    /// </summary>
    Task<List<NotificationResponse>> GetUserNotificationsAsync(
        int userId,
        bool unreadOnly = false,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets unread notification count for user.
    /// </summary>
    Task<int> GetUnreadCountAsync(int userId);

    /// <summary>
    /// Marks one notification as read.
    /// </summary>
    Task<bool> MarkAsReadAsync(int userId, int notificationId);

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    Task<int> MarkAllAsReadAsync(int userId);

    /// <summary>
    /// Updates notification settings for user.
    /// </summary>
    Task<bool> UpdateNotificationSettingsAsync(int userId, NotificationSettingsDto settings);

    /// <summary>
    /// Gets notification settings for user.
    /// </summary>
    Task<NotificationSettingsDto> GetNotificationSettingsAsync(int userId);
}
