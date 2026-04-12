namespace API.Application.DTOs;

/// <summary>
/// DTO for searching flights with specific criteria.
/// Used as input for flight search operations.
/// Allows filtering by route, date, and passenger requirements.
/// </summary>
public class FlightSearchDto
{
    /// <summary>ID of the departure airport to filter by.</summary>
    public int DepartureAirportId { get; set; }

    /// <summary>ID of the arrival airport to filter by.</summary>
    public int ArrivalAirportId { get; set; }

    /// <summary>Departure date to search for flights (typically date portion only).</summary>
    public DateTime DepartureDate { get; set; }

    /// <summary>Optional seat class filter (e.g., "Economy", "Business", "FirstClass"). Null returns all classes.</summary>
    public string? SeatClass { get; set; }

    /// <summary>Number of passengers to filter by seat availability.</summary>
    public int PassengerCount { get; set; }
}
