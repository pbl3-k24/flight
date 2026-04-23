namespace API.Domain.Booking;

public class Booking
{
    public Guid Id { get; set; }

    public string BookingCode { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "VND";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAtUtc { get; set; }

    public ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();

    public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
