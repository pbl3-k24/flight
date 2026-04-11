namespace FlightBooking.Domain.Entities;

public class PriceRule
{
    public Guid Id { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? FareClassId { get; set; }
    public string? Season { get; set; }
    public string? DayOfWeek { get; set; } // Mon,Tue,...
    public int? DaysBeforeDeparture { get; set; }
    public decimal BasePrice { get; set; }
    public decimal Multiplier { get; set; } = 1.0m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Route? Route { get; set; }
}
