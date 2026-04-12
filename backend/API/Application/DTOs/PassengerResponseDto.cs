namespace API.Application.DTOs;

/// <summary>
/// DTO for returning passenger information.
/// </summary>
public class PassengerResponseDto
{
    /// <summary>Passenger ID.</summary>
    public int Id { get; set; }

    /// <summary>Passenger's first name.</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>Passenger's last name.</summary>
    public string LastName { get; set; } = null!;

    /// <summary>Passenger's date of birth.</summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>Passenger's email address.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Passenger's phone number.</summary>
    public string? PhoneNumber { get; set; }
}
