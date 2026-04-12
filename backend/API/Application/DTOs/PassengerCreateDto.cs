namespace API.Application.DTOs;

/// <summary>
/// DTO for creating a passenger within a booking.
/// </summary>
public class PassengerCreateDto
{
    /// <summary>Passenger's first name.</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>Passenger's last name.</summary>
    public string LastName { get; set; } = null!;

    /// <summary>Passenger's date of birth.</summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>Passenger's passport number (required for international flights).</summary>
    public string? PassportNumber { get; set; }

    /// <summary>Passenger's nationality code.</summary>
    public string? Nationality { get; set; }

    /// <summary>Passenger's email address.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Passenger's phone number.</summary>
    public string? PhoneNumber { get; set; }
}
