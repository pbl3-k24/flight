namespace API.Application.Dtos.Admin;

public class CreateFlightDto
{
    public string FlightNumber { get; set; } = null!;

    public int RouteId { get; set; }

    public int AircraftId { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateFlightDto
{
    public string? FlightNumber { get; set; }

    public int? AircraftId { get; set; }

    public DateTime? DepartureTime { get; set; }

    public DateTime? ArrivalTime { get; set; }

    public bool? IsActive { get; set; }
}

public class FlightManagementResponse
{
    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string RouteCode { get; set; } = null!;

    public string AircraftModel { get; set; } = null!;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public int BookedSeats { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CreateRouteDto
{
    public int DepartureAirportId { get; set; }

    public int ArrivalAirportId { get; set; }

    public int DistanceKm { get; set; }

    public int EstimatedDurationMinutes { get; set; }
}

public class UpdateRouteDto
{
    public int? DepartureAirportId { get; set; }

    public int? ArrivalAirportId { get; set; }

    public int? DistanceKm { get; set; }

    public int? EstimatedDurationMinutes { get; set; }
}

public class RouteManagementResponse
{
    public int RouteId { get; set; }

    public string DepartureAirport { get; set; } = null!;

    public string ArrivalAirport { get; set; } = null!;

    public int DistanceKm { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public int ActiveFlights { get; set; }

    public bool IsActive { get; set; }
}
