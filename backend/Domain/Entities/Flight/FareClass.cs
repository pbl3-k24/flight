using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Flight;

public class FareClass : BaseEntity
{
    public FareClassCode Code { get; set; }           // Economy, Business
    public string DisplayName { get; set; } = string.Empty;
    public int FreeBaggageKg { get; set; }
    public bool MealIncluded { get; set; }
    public bool RefundAllowed { get; set; }
    public decimal RefundFeePercent { get; set; }     // 0-100
    public bool ChangeDateAllowed { get; set; }
    public decimal ChangeDateFeePercent { get; set; }
    public string? Description { get; set; }

    // Navigation
    public ICollection<FlightInventory> Inventories { get; set; } = new List<FlightInventory>();
    public ICollection<FlightFarePrice> FarePrices { get; set; } = new List<FlightFarePrice>();
    public ICollection<Booking.BookingItem> BookingItems { get; set; } = new List<Booking.BookingItem>();
}
