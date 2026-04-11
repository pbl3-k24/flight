namespace FlightBooking.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "active"; // active, suspended, deleted
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<OAuthAccount> OAuthAccounts { get; set; } = [];
    public ICollection<OtpToken> OtpTokens { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
