namespace API.Domain.Entities;

public class Route
{
    public int Id { get; set; }

    public int DepartureAirportId { get; set; }

    public int ArrivalAirportId { get; set; }

    public int DistanceKm { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Airport DepartureAirport { get; set; } = null!;

    public virtual Airport ArrivalAirport { get; set; } = null!;

    public virtual ICollection<Flight> Flights { get; set; } = [];

    // Domain methods
    public double GetDurationInHours() => EstimatedDurationMinutes / 60.0;

    public bool IsValid() => DepartureAirportId != ArrivalAirportId;
}
