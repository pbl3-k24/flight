using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Flight;

/// <summary>
/// Effective current price for a flight + fare class combination.
/// Updated by auto-pricing engine or admin override.
/// </summary>
public class FlightFarePrice : BaseEntity
{
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "VND";
    public PriceSource Source { get; set; } = PriceSource.Auto;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    // Navigation
    public Flight Flight { get; set; } = null!;
    public FareClass FareClass { get; set; } = null!;
    public ICollection<PriceOverrideLog> OverrideLogs { get; set; } = new List<PriceOverrideLog>();
}
