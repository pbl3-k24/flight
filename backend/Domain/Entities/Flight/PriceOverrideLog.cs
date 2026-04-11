using Domain.Common;

namespace Domain.Entities.Flight;

/// <summary>
/// Immutable audit trail for every admin price override.
/// </summary>
public class PriceOverrideLog : BaseEntity
{
    public Guid FlightFarePriceId { get; set; }
    public Guid AdminUserId { get; set; }
    public decimal PriceBefore { get; set; }
    public decimal PriceAfter { get; set; }
    public string? Reason { get; set; }

    // Navigation
    public FlightFarePrice FlightFarePrice { get; set; } = null!;
}
