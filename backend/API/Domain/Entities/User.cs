using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class User
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? GoogleId { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public bool IsActive() => Status == 0;

    public bool IsSuspended() => Status == 2;

    public void UpdatePassword(string newHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string? phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        if (fullName.Length > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(fullName));
        }

        if (phone is { Length: > 20 })
        {
            throw new ArgumentOutOfRangeException(nameof(phone));
        }

        FullName = fullName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }
}
