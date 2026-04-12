namespace API.Domain.Enums;

/// <summary>
/// Represents the status of a flight in the system.
/// </summary>
public enum FlightStatus
{
    /// <summary>Flight is active and available for bookings.</summary>
    Active = 1,

    /// <summary>Flight has been cancelled and is not available for bookings.</summary>
    Cancelled = 2,

    /// <summary>Flight is delayed and may not depart on schedule.</summary>
    Delayed = 3,

    /// <summary>Flight has been completed and all passengers have arrived.</summary>
    Completed = 4
}
