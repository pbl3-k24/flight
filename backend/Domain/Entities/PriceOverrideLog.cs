namespace FlightBooking.Domain.Entities;

public class PriceOverrideLog
{
    public Guid Id { get; set; }
    public Guid FlightFarePriceId { get; set; }
    public Guid AdminId { get; set; }
    public decimal PriceBefore { get; set; }
    public decimal PriceAfter { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public FlightFarePrice FlightFarePrice { get; set; } = null!;
}
