namespace FlightBooking.Domain.Entities;

public class OtpToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty; // email_verification, password_reset
    public bool IsUsed { get; set; }
    public int RetryCount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
