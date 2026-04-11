namespace FlightBooking.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? IdentityNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
