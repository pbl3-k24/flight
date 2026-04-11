namespace FlightBooking.Domain.Entities;

public class SeatTemplate
{
    public Guid Id { get; set; }
    public Guid AircraftId { get; set; }
    public string SeatNumber { get; set; } = string.Empty; // 1A, 1B, ...
    public string FareClassCode { get; set; } = string.Empty; // ECO, BUS
    public string? SeatType { get; set; } // window, middle, aisle
    public bool IsActive { get; set; } = true;

    public Aircraft Aircraft { get; set; } = null!;
}
