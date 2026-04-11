using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Booking;

public class Booking : BaseEntity
{
    public string BookingCode { get; set; } = string.Empty;  // Unique reference e.g. VN-2024-ABCDEF
    public Guid UserId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime? ExpiresAt { get; set; }             // Hold seat timeout
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public User.User User { get; set; } = null!;
    public ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
    public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
    public ICollection<Payment.Payment> Payments { get; set; } = new List<Payment.Payment>();
}
