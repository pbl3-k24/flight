using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.User;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public bool EmailVerified { get; set; } = false;

    // Navigation properties
    public UserProfile? Profile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<OAuthAccount> OAuthAccounts { get; set; } = new List<OAuthAccount>();
    public ICollection<Booking.Booking> Bookings { get; set; } = new List<Booking.Booking>();
}
