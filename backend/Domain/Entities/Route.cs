namespace FlightBooking.Domain.Entities;

public class Route
{
    public Guid Id { get; set; }
    public Guid OriginAirportId { get; set; }
    public Guid DestinationAirportId { get; set; }
    public bool IsDomestic { get; set; } = true;
    public int? DistanceKm { get; set; }
    public bool IsActive { get; set; } = true;

    public Airport OriginAirport { get; set; } = null!;
    public Airport DestinationAirport { get; set; } = null!;
    public ICollection<Flight> Flights { get; set; } = [];
    public ICollection<PriceRule> PriceRules { get; set; } = [];
}
