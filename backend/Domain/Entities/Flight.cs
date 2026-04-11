namespace FlightBooking.Domain.Entities;

public class Flight
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty; // VJ123
    public Guid RouteId { get; set; }
    public Guid AircraftId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Status { get; set; } = "scheduled"; // scheduled, boarding, departed, arrived, cancelled, delayed
    public string? GateNumber { get; set; }
    public string? DelayReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Route Route { get; set; } = null!;
    public Aircraft Aircraft { get; set; } = null!;
    public ICollection<FlightInventory> Inventories { get; set; } = [];
    public ICollection<FlightFarePrice> FarePrices { get; set; } = [];
    public ICollection<BookingItem> BookingItems { get; set; } = [];
}
