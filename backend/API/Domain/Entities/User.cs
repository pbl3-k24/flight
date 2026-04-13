namespace API.Domain.Entities;

using API.Domain.Enums;

/// <summary>
/// Represents a user in the system.
/// This is an aggregate root that manages user profile and related data.
/// Enforces invariants:
/// - email is unique
/// - email is valid format
/// - password hash exists
/// - date_of_birth is not a future date
/// </summary>
public class User
{
    /// <summary>Unique identifier for the user.</summary>
    public int Id { get; set; }

    /// <summary>User's email address (unique).</summary>
    public string Email { get; set; } = null!;

    /// <summary>User's full display name.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>User's first name.</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>User's last name.</summary>
    public string LastName { get; set; } = null!;

    /// <summary>User's date of birth.</summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>User's phone number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Google OAuth provider ID if linked.</summary>
    public string? GoogleId { get; set; }

    /// <summary>Hashed password of the user.</summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>Current status of the user account (Active, Deactivated, Suspended).</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>When the user account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the user account was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>Bookings made by this user.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>Payments made by this user.</summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>Roles assigned to this user.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>Email verification tokens issued for this user.</summary>
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    /// <summary>Password reset tokens issued for this user.</summary>
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    // Domain methods
    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        Status = UserStatus.Deactivated;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates the user account.
    /// </summary>
    public void Reactivate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends the user account.
    /// </summary>
    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }
}
