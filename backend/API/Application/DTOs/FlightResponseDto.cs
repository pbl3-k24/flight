namespace API.Application.DTOs;

/// <summary>
/// DTO for returning flight information.
/// Transfers data from the application layer to the API layer.
/// Contains flight details with current status and seat availability.
/// </summary>
public class FlightResponseDto
{
    /// <summary>Unique flight identifier.</summary>
    public int Id { get; set; }

    /// <summary>Flight number/designation (e.g., "AA100").</summary>
    public string FlightNumber { get; set; } = null!;

    /// <summary>ID of the departing airport.</summary>
    public int DepartureAirportId { get; set; }

    /// <summary>Name of the departing airport.</summary>
    public string DepartureAirportName { get; set; } = null!;

    /// <summary>IATA code of the departing airport (e.g., "LAX").</summary>
    public string DepartureAirportCode { get; set; } = null!;

    /// <summary>ID of the arrival airport.</summary>
    public int ArrivalAirportId { get; set; }

    /// <summary>Name of the arrival airport.</summary>
    public string ArrivalAirportName { get; set; } = null!;

    /// <summary>IATA code of the arrival airport (e.g., "JFK").</summary>
    public string ArrivalAirportCode { get; set; } = null!;

    /// <summary>Scheduled departure date and time.</summary>
    public DateTime DepartureTime { get; set; }

    /// <summary>Scheduled arrival date and time.</summary>
    public DateTime ArrivalTime { get; set; }

    /// <summary>Airline operating the flight.</summary>
    public string Airline { get; set; } = null!;

    /// <summary>Aircraft model used for this flight.</summary>
    public string AircraftModel { get; set; } = null!;

    /// <summary>Total number of seats on the flight.</summary>
    public int TotalSeats { get; set; }

    /// <summary>Number of seats currently available for booking.</summary>
    public int AvailableSeats { get; set; }

    /// <summary>Base price per seat for this flight.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Current status of the flight (Active, Cancelled, Delayed, Completed).</summary>
    public string Status { get; set; } = null!;

    /// <summary>When the flight was created in the system.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the flight information was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
}
