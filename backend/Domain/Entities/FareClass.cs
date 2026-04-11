namespace FlightBooking.Domain.Entities;

public class FareClass
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // ECO, BUS, PROMO
    public string Name { get; set; } = string.Empty;
    public int CheckedBaggageKg { get; set; }
    public int CabinBaggageKg { get; set; }
    public bool IsRefundable { get; set; }
    public decimal RefundFeePercent { get; set; }
    public bool IsChangeable { get; set; }
    public decimal ChangeFee { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FlightInventory> Inventories { get; set; } = [];
    public ICollection<FlightFarePrice> FarePrices { get; set; } = [];
    public ICollection<BookingItem> BookingItems { get; set; } = [];
}
