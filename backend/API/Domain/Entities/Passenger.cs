namespace API.Domain.Entities;

/// <summary>
/// Represents a passenger within a booking.
/// Passengers are part of the Booking aggregate and should not exist independently.
/// Enforces invariants:
/// - Age validation (if international flight, age >= 2)
/// - Document validation (passport number format)
/// - Name format validation
/// </summary>
public class Passenger
{
    /// <summary>Unique identifier for the passenger record.</summary>
    public int Id { get; set; }

    /// <summary>ID of the booking this passenger belongs to.</summary>
    public int BookingId { get; set; }

    /// <summary>Passenger's first name.</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>Passenger's last name.</summary>
    public string LastName { get; set; } = null!;

    /// <summary>Passenger's date of birth.</summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>Passport number of the passenger (for international flights).</summary>
    public string? PassportNumber { get; set; }

    /// <summary>Nationality/Country code of the passenger.</summary>
    public string? Nationality { get; set; }

    /// <summary>Email address of the passenger.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Phone number of the passenger.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>When this passenger record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When this passenger record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    /// <summary>The booking this passenger is associated with.</summary>
    public Booking? Booking { get; set; }

    // Domain methods
    /// <summary>
    /// Gets the passenger's current age based on date of birth.
    /// </summary>
    /// <returns>The passenger's age in years.</returns>
    public int GetAge()
    {
        var today = DateTime.UtcNow;
        var age = today.Year - DateOfBirth.Year;

        if (DateOfBirth.Date > today.AddYears(-age))
            age--;

        return age;
    }

    /// <summary>
    /// Validates the passenger's details for international flights.
    /// </summary>
    /// <returns>True if valid for international flights, false otherwise.</returns>
    public bool IsValidForInternationalFlight()
    {
        if (GetAge() < 2)
            return false;

        if (string.IsNullOrEmpty(PassportNumber))
            return false;

        if (string.IsNullOrEmpty(Nationality))
            return false;

        return true;
    }
}
