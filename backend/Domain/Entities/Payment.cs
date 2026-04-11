namespace FlightBooking.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string Gateway { get; set; } = string.Empty; // vnpay, momo, zalopay
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "pending"; // pending, completed, failed, cancelled
    public string? TransactionRef { get; set; }
    public string? GatewayTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public ICollection<PaymentEvent> Events { get; set; } = [];
    public ICollection<Refund> Refunds { get; set; } = [];
}
