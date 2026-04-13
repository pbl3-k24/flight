namespace API.Domain.Entities;

/// <summary>
/// One-time token used to reset a user's password.
/// </summary>
public class PasswordResetToken
{
    /// <summary>Token identifier.</summary>
    public int Id { get; set; }

    /// <summary>User identifier.</summary>
    public int UserId { get; set; }

    /// <summary>Reset code.</summary>
    public string Code { get; set; } = null!;

    /// <summary>Expiration timestamp.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Used timestamp; null if unused.</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>User navigation.</summary>
    public User? User { get; set; }
}
