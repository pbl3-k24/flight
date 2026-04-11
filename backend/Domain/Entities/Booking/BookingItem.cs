using Domain.Common;

namespace Domain.Entities.Booking;

/// <summary>
/// One row per passenger per flight in a booking.
/// </summary>
public class BookingItem : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public string? SeatNumber { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "VND";

    // Navigation
    public Booking Booking { get; set; } = null!;
    public Passenger Passenger { get; set; } = null!;
    public Flight.Flight Flight { get; set; } = null!;
    public Flight.FareClass FareClass { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}
