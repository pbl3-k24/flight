using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [MaxLength(500)]
    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired(DateTime currentDateTime) => currentDateTime >= ExpiresAt;

    public bool IsUsed() => UsedAt.HasValue;

    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }
}
