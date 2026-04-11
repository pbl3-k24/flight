using Domain.Common;

namespace Domain.Entities.Flight;

public class Aircraft : BaseEntity
{
    public string RegistrationCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;  // A320, B737, ATR72
    public int TotalSeats { get; set; }
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();
}
