namespace API.Domain.Entities;

public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    // Domain methods
    public bool IsExpired(DateTime currentDateTime) => currentDateTime > ExpiresAt;

    public bool IsUsed() => UsedAt.HasValue;

    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }
}
