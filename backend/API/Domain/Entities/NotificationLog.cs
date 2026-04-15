namespace API.Domain.Entities;

public class NotificationLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Email { get; set; }

    public string Type { get; set; } = null!; // EMAIL, SMS, PUSH

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int Status { get; set; } = 0; // 0=Pending, 1=Sent, 2=Failed

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    // Domain methods
    public void MarkAsSent()
    {
        Status = 1; // Sent
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = 2; // Failed
    }

    public bool IsPending() => Status == 0;
}
