namespace API.Application.DTOs;

/// <summary>
/// DTO for updating an existing flight.
/// Transferred from client requests to the application layer.
/// Only contains fields that can be updated (not ID, Status, or timestamps).
/// </summary>
public class FlightUpdateDto
{
    /// <summary>Updated flight number/designation (e.g., "AA100").</summary>
    public string? FlightNumber { get; set; }

    /// <summary>Updated departure airport ID.</summary>
    public int? DepartureAirportId { get; set; }

    /// <summary>Updated arrival airport ID.</summary>
    public int? ArrivalAirportId { get; set; }

    /// <summary>Updated scheduled departure date and time.</summary>
    public DateTime? DepartureTime { get; set; }

    /// <summary>Updated scheduled arrival date and time.</summary>
    public DateTime? ArrivalTime { get; set; }

    /// <summary>Updated airline name.</summary>
    public string? Airline { get; set; }

    /// <summary>Updated aircraft model.</summary>
    public string? AircraftModel { get; set; }

    /// <summary>Updated total number of seats available on the flight.</summary>
    public int? TotalSeats { get; set; }

    /// <summary>Updated base price per seat for this flight.</summary>
    public decimal? BasePrice { get; set; }
}
