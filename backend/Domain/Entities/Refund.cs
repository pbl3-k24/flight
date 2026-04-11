namespace FlightBooking.Domain.Entities;

public class Refund
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? BookingItemId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed, rejected
    public string? GatewayRef { get; set; }
    public Guid? ProcessedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Payment Payment { get; set; } = null!;
}
