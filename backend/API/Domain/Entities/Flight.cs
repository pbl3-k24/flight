using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class Flight
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string FlightNumber { get; set; } = string.Empty;

    public int RouteId { get; set; }
    public int AircraftId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Route Route { get; set; } = null!;
    public Aircraft Aircraft { get; set; } = null!;
    public ICollection<FlightSeatInventory> SeatInventories { get; set; } = new List<FlightSeatInventory>();
    public ICollection<Booking> OutboundBookings { get; set; } = new List<Booking>();
    public ICollection<Booking> ReturnBookings { get; set; } = new List<Booking>();

    public Airport? GetDepartureAirport() => Route?.DepartureAirport;

    public Airport? GetArrivalAirport() => Route?.ArrivalAirport;

    public TimeSpan GetDuration() => ArrivalTime - DepartureTime;

    public bool CanBook(int seatClassId, int count)
        => SeatInventories.FirstOrDefault(x => x.SeatClassId == seatClassId)?.CanBook(count) ?? false;

    public void ReserveSeats(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(x => x.SeatClassId == seatClassId)
            ?? throw new InvalidOperationException("Seat inventory not found.");
        inventory.ReserveSeats(count);
    }

    public void ReleaseSeats(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(x => x.SeatClassId == seatClassId)
            ?? throw new InvalidOperationException("Seat inventory not found.");
        inventory.ReleaseHold(count);
    }

    public void Cancel()
    {
        Status = 1;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDepartureSoon(int hours)
    {
        return DepartureTime <= DateTime.UtcNow.AddHours(hours);
    }
}
