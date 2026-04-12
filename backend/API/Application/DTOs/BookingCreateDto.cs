namespace API.Application.DTOs;

/// <summary>
/// DTO for creating a new booking.
/// Transferred from client requests to the application layer.
/// </summary>
public class BookingCreateDto
{
    /// <summary>ID of the flight to book.</summary>
    public int FlightId { get; set; }

    /// <summary>Number of passengers in the booking.</summary>
    public int PassengerCount { get; set; }

    /// <summary>Optional special requests or notes for the booking.</summary>
    public string? Notes { get; set; }

    /// <summary>Passenger details for the booking.</summary>
    public List<PassengerCreateDto> Passengers { get; set; } = new();
}
