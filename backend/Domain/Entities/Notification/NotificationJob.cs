using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Notification;

public class NotificationJob : BaseEntity
{
    public NotificationJobType JobType { get; set; }
    public NotificationJobStatus Status { get; set; } = NotificationJobStatus.Queued;
    public string RecipientEmail { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";      // JSON data for template rendering
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? RelatedBookingId { get; set; }
    public Guid? RelatedUserId { get; set; }
}
