namespace FlightBooking.Domain.Entities;

public class PaymentEvent
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string EventType { get; set; } = string.Empty; // payment_initiated, payment_success, payment_failed, webhook_received
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; }

    public Payment Payment { get; set; } = null!;
}
