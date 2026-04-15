namespace API.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? GoogleId { get; set; }

    public int Status { get; set; } = 0; // 0=Active, 1=Inactive, 2=Suspended

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Role> Roles { get; set; } = [];

    public virtual ICollection<Booking> Bookings { get; set; } = [];

    public virtual ICollection<UserRole> UserRoles { get; set; } = [];

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = [];

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];

    public virtual ICollection<NotificationLog> NotificationLogs { get; set; } = [];

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = [];

    // Domain methods
    public bool IsActive() => Status == 0;

    public bool IsSuspended() => Status == 2;

    public void UpdatePassword(string newHash)
    {
        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string? phone)
    {
        FullName = fullName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }
}
