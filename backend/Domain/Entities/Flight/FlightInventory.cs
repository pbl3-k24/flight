using Domain.Common;

namespace Domain.Entities.Flight;

/// <summary>
/// Tracks seat availability per flight per fare class.
/// </summary>
public class FlightInventory : BaseEntity
{
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int HeldSeats { get; set; }       // Temporarily reserved pending payment
    public int SoldSeats { get; set; }

    // Navigation
    public Flight Flight { get; set; } = null!;
    public FareClass FareClass { get; set; } = null!;
}
