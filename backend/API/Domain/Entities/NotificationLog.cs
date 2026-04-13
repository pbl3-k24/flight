using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class NotificationLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Email { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;

    public void MarkAsSent()
    {
        Status = 1;
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed() => Status = 2;

    public bool IsPending() => Status == 0;
}
