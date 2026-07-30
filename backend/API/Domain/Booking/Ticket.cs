namespace API.Domain.Booking;

public class Ticket
{
    public Guid Id { get; set; }

    public string TicketNo { get; set; } = string.Empty;

    public Guid BookingItemId { get; set; }

    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

    public TicketStatus Status { get; set; } = TicketStatus.Issued;
}
