namespace FlightBooking.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public string BookingCode { get; set; } = string.Empty; // e.g. BK20240101001
    public Guid UserId { get; set; }
    public string Status { get; set; } = "pending_payment"; // pending_payment, confirmed, cancelled, expired
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime? ExpiresAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Passenger> Passengers { get; set; } = [];
    public ICollection<BookingItem> Items { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
