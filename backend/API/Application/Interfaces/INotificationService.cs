namespace API.Application.Interfaces;

using API.Application.Dtos.Notification;

public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a user.
    /// </summary>
    Task<bool> SendNotificationAsync(int userId, string subject, string message, string type = "EMAIL");

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
    /// Gets notification history for user.
    /// </summary>
    Task<List<NotificationResponse>> GetUserNotificationsAsync(int userId);

    /// <summary>
    /// Updates notification settings for user.
    /// </summary>
    Task<bool> UpdateNotificationSettingsAsync(int userId, NotificationSettingsDto settings);

    /// <summary>
    /// Gets notification settings for user.
    /// </summary>
    Task<NotificationSettingsDto> GetNotificationSettingsAsync(int userId);
}
