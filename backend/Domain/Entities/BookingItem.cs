namespace FlightBooking.Domain.Entities;

public class BookingItem
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public string? SeatNumber { get; set; }
    public decimal Price { get; set; }
    public decimal TaxAndFee { get; set; }
    public string Status { get; set; } = "active"; // active, cancelled

    public Booking Booking { get; set; } = null!;
    public Passenger Passenger { get; set; } = null!;
    public Flight Flight { get; set; } = null!;
    public FareClass FareClass { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}
