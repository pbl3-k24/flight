namespace FlightBooking.Domain.Entities;

public class Aircraft
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // tail number / registration
    public string Model { get; set; } = string.Empty; // A321, B737
    public string? Manufacturer { get; set; }
    public int TotalSeats { get; set; }
    public string? SeatMapVersion { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SeatTemplate> SeatTemplates { get; set; } = [];
    public ICollection<Flight> Flights { get; set; } = [];
}
