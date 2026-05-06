namespace API.Application.Dtos.FlightDefinition;

public class FlightDefinitionDto
{
    public int Id { get; set; }
    public string FlightNumber { get; set; } = null!;
    public int RouteId { get; set; }
    public int DefaultAircraftId { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public int ArrivalOffsetDays { get; set; }
    public int OperatingDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation info
    public string? RouteName { get; set; }
    public string? DepartureAirportCode { get; set; }
    public string? ArrivalAirportCode { get; set; }
    public string? AircraftModel { get; set; }
    
    // Computed
    public bool IsOvernightFlight { get; set; }
    public string? OperatingDaysText { get; set; }
}

public class CreateFlightDefinitionDto
{
    public string FlightNumber { get; set; } = null!;
    public int RouteId { get; set; }
    public int DefaultAircraftId { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public int ArrivalOffsetDays { get; set; } = 0;
    public int OperatingDays { get; set; } = 127; // Default: every day
    public bool IsActive { get; set; } = true;
}

public class UpdateFlightDefinitionDto
{
    public int RouteId { get; set; }
    public int DefaultAircraftId { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public int ArrivalOffsetDays { get; set; }
    public int OperatingDays { get; set; }
    public bool IsActive { get; set; }
}
