namespace API.Domain.Entities;

public class Route
{
    public int Id { get; set; }
    public int DepartureAirportId { get; set; }
    public int ArrivalAirportId { get; set; }
    public int DistanceKm { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public Airport DepartureAirport { get; set; } = null!;
    public Airport ArrivalAirport { get; set; } = null!;
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();

    public double GetDurationInHours() => EstimatedDurationMinutes / 60.0;

    public bool IsValid() => DepartureAirportId != ArrivalAirportId && DistanceKm > 0 && EstimatedDurationMinutes > 0;
}
