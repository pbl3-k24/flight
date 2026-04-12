namespace API.Domain.Enums;

/// <summary>
/// Represents the status of a booking in the system.
/// </summary>
public enum BookingStatus
{
    /// <summary>Booking is pending payment confirmation.</summary>
    Pending = 1,

    /// <summary>Booking has been confirmed and payment received.</summary>
    Confirmed = 2,

    /// <summary>Passenger has checked in for the flight.</summary>
    CheckedIn = 3,

    /// <summary>Booking has been cancelled by user or system.</summary>
    Cancelled = 4
}
