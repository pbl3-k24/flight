namespace FlightBooking.Domain.Entities;

public class FlightFarePrice
{
    public Guid Id { get; set; }
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public decimal CurrentPrice { get; set; }
    public string PriceSource { get; set; } = "auto"; // auto, manual
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Flight Flight { get; set; } = null!;
    public FareClass FareClass { get; set; } = null!;
    public ICollection<PriceOverrideLog> OverrideLogs { get; set; } = [];
}
