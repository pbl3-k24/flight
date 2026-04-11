using Domain.Common;

namespace Domain.Entities.Flight;

/// <summary>
/// Dynamic pricing rules that auto-engine uses to compute base prices.
/// </summary>
public class PriceRule : BaseEntity
{
    public Guid? RouteId { get; set; }               // null = applies to all routes
    public decimal BasePrice { get; set; }
    public decimal Multiplier { get; set; } = 1.0m;  // Applied on top of base
    public int? DayOfWeek { get; set; }              // 0=Sun .. 6=Sat, null = all days
    public int? SeasonMonth { get; set; }            // 1-12, null = all months
    public int? DaysBeforeDeparture { get; set; }   // e.g. 0-7: last-minute, null = ignore
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;

    // Navigation
    public Route? Route { get; set; }
}
