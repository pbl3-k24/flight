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

    // Security properties
    public bool IsEmailVerified { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsTwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public bool PhoneNumberVerified { get; set; } = false;

    // Profile properties
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    public string? PassportCountry { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Occupation { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredCurrency { get; set; }
    public string? TimeZone { get; set; }
    public bool MarketingOptIn { get; set; } = false;
    public bool NewsletterSubscription { get; set; } = false;
    public string? NotificationPreferences { get; set; }

    // Audit properties
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Concurrency token
    public int Version { get; set; } = 0;

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
        PasswordChangedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string? phone)
    {
        FullName = fullName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkEmailAsVerified()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableTwoFactor(string secret)
    {
        IsTwoFactorEnabled = true;
        TwoFactorSecret = secret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableTwoFactor()
    {
        IsTwoFactorEnabled = false;
        TwoFactorSecret = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPhoneNumberAsVerified()
    {
        PhoneNumberVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
