using Domain.Common;

namespace Domain.Entities.Flight;

public class Route : BaseEntity
{
    public Guid OriginAirportId { get; set; }
    public Guid DestinationAirportId { get; set; }
    public bool IsDomestic { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Airport OriginAirport { get; set; } = null!;
    public Airport DestinationAirport { get; set; } = null!;
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    public ICollection<PriceRule> PriceRules { get; set; } = new List<PriceRule>();
}
