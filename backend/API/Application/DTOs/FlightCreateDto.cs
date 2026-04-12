namespace API.Application.DTOs;

/// <summary>
/// DTO for creating a new flight.
/// Transferred from client requests to the application layer.
/// No ID or Status properties - these are assigned by the system.
/// </summary>
public class FlightCreateDto
{
    /// <summary>Flight number/designation (e.g., "AA100").</summary>
    public string FlightNumber { get; set; } = null!;

    /// <summary>ID of the departing airport.</summary>
    public int DepartureAirportId { get; set; }

    /// <summary>ID of the arrival airport.</summary>
    public int ArrivalAirportId { get; set; }

    /// <summary>Scheduled departure date and time.</summary>
    public DateTime DepartureTime { get; set; }

    /// <summary>Scheduled arrival date and time.</summary>
    public DateTime ArrivalTime { get; set; }

    /// <summary>Total number of seats available on the flight.</summary>
    public int TotalSeats { get; set; }

    /// <summary>Base price per seat for this flight.</summary>
    public decimal BasePrice { get; set; }
}
