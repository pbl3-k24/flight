namespace API.Domain.Enums;

/// <summary>
/// Represents the status of a payment in the system.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment is pending processing.</summary>
    Pending = 1,

    /// <summary>Payment has been successfully processed.</summary>
    Completed = 2,

    /// <summary>Payment has failed.</summary>
    Failed = 3,

    /// <summary>Payment has been refunded.</summary>
    Refunded = 4
}
