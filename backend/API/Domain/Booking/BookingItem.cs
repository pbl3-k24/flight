namespace API.Domain.Booking;

public class BookingItem
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid FlightId { get; set; }

    public string FareClassCode { get; set; } = string.Empty;

    public string? SeatNo { get; set; }

    public decimal Price { get; set; }
}
