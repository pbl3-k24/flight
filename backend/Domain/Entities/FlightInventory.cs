namespace FlightBooking.Domain.Entities;

public class FlightInventory
{
    public Guid Id { get; set; }
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int HeldSeats { get; set; }
    public int SoldSeats { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Flight Flight { get; set; } = null!;
    public FareClass FareClass { get; set; } = null!;
}
