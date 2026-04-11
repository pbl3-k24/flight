namespace FlightBooking.Domain.Entities;

public class NotificationJob
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // email, sms
    public string Recipient { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}"; // JSON
    public string Status { get; set; } = "pending"; // pending, sent, failed
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? ErrorMessage { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
