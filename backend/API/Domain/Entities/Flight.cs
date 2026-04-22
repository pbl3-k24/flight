namespace API.Domain.Entities;

public class Flight
{
    public int Id { get; set; }

    public string FlightNumber { get; set; } = null!;

    public int RouteId { get; set; }

    public int AircraftId { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int Status { get; set; } = 0; // 0=Active, 1=Cancelled, 2=Delayed, 3=Completed

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual Route Route { get; set; } = null!;

    public virtual Aircraft Aircraft { get; set; } = null!;

    public virtual ICollection<FlightSeatInventory> SeatInventories { get; set; } = [];

    public virtual ICollection<Booking> OutboundBookings { get; set; } = [];

    public virtual ICollection<Booking> ReturnBookings { get; set; } = [];

    // Domain methods
    public Airport GetDepartureAirport() => Route.DepartureAirport;

    public Airport GetArrivalAirport() => Route.ArrivalAirport;

    public TimeSpan GetDuration() => ArrivalTime - DepartureTime;

    public bool CanBook(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(si => si.SeatClassId == seatClassId);
        return inventory?.CanHold(count) ?? false;
    }

    public void ReserveSeats(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(si => si.SeatClassId == seatClassId);
        if (inventory != null)
        {
            inventory.HoldSeats(count);
        }
    }

    public void ReleaseSeats(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(si => si.SeatClassId == seatClassId);
        if (inventory != null)
        {
            inventory.ReleaseHeldSeats(count);
        }
    }

    public void Cancel()
    {
        Status = 1; // Cancelled
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDepartureSoon(int hours)
    {
        var now = DateTime.UtcNow;
        var timeUntilDeparture = DepartureTime - now;
        return timeUntilDeparture.TotalHours <= hours && timeUntilDeparture.TotalHours > 0;
    }
}
