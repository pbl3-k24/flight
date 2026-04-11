namespace FlightBooking.Domain.Entities;

public class OAuthAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty; // google
    public string ProviderUserId { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
