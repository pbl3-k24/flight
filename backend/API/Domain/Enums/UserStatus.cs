namespace API.Domain.Enums;

/// <summary>
/// Represents the status of a user in the system.
/// </summary>
public enum UserStatus
{
    /// <summary>User account is active and can perform operations.</summary>
    Active = 1,

    /// <summary>User account is deactivated and cannot perform operations.</summary>
    Deactivated = 2,

    /// <summary>User account is suspended due to violations.</summary>
    Suspended = 3
}
