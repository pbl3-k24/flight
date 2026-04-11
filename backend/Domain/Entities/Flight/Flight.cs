using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Flight;

public class Flight : BaseEntity
{
    public string FlightNumber { get; set; } = string.Empty;  // VN123
    public Guid RouteId { get; set; }
    public Guid AircraftId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;
    public string? GateNumber { get; set; }
    public string? Terminal { get; set; }

    // Navigation
    public Route Route { get; set; } = null!;
    public Aircraft Aircraft { get; set; } = null!;
    public ICollection<FlightInventory> Inventories { get; set; } = new List<FlightInventory>();
    public ICollection<FlightFarePrice> FarePrices { get; set; } = new List<FlightFarePrice>();
    public ICollection<Booking.BookingItem> BookingItems { get; set; } = new List<Booking.BookingItem>();
}
