using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Payment;

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionRef { get; set; }       // Gateway transaction reference
    public string? GatewayOrderId { get; set; }       // Order ID sent to gateway
    public string? IdempotencyKey { get; set; }        // Prevent duplicate charges
    public DateTime? PaidAt { get; set; }

    // Navigation
    public Booking.Booking Booking { get; set; } = null!;
    public ICollection<PaymentEvent> Events { get; set; } = new List<PaymentEvent>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
