namespace API.Application.Dtos.Notification;

public class SendNotificationDto
{
    public int UserId { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!; // EMAIL, SMS, PUSH, IN_APP
}

public class NotificationResponse
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string UserEmail { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Status { get; set; } = null!; // SENT, PENDING, FAILED

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ErrorMessage { get; set; }
}

public class NotificationSettingsDto
{
    public bool EmailNotifications { get; set; } = true;

    public bool SmsNotifications { get; set; } = false;

    public bool PushNotifications { get; set; } = true;

    public bool BookingConfirmation { get; set; } = true;

    public bool PaymentReminder { get; set; } = true;

    public bool RefundNotification { get; set; } = true;

    public bool PromoNotifications { get; set; } = false;
}

public class NotificationTemplateDto
{
    public string Key { get; set; } = null!; // BOOKING_CONFIRMED, PAYMENT_RECEIVED, etc.

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public Dictionary<string, string>? Variables { get; set; }
}
