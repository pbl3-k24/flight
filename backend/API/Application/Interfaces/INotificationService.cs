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

public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message.
    /// </summary>
    Task<bool> SendSmsAsync(string phoneNumber, string message);

    /// <summary>
    /// Sends OTP via SMS.
    /// </summary>
    Task<bool> SendOtpAsync(string phoneNumber, string otp);
}

public interface IPushNotificationService
{
    /// <summary>
    /// Sends a push notification.
    /// </summary>
    Task<bool> SendPushAsync(int userId, string title, string body);

    /// <summary>
    /// Sends bulk push notifications.
    /// </summary>
    Task<bool> SendBulkPushAsync(List<int> userIds, string title, string body);
}
