namespace FlightBooking.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty; // TK-20240101-00001
    public Guid BookingItemId { get; set; }
    public string Status { get; set; } = "issued"; // issued, used, cancelled, refunded
    public DateTime IssuedAt { get; set; }
    public string? BoardingPassUrl { get; set; }
    public string? ETicketUrl { get; set; }

    public BookingItem BookingItem { get; set; } = null!;
}
