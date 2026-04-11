namespace FlightBooking.Domain.Entities;

/// <summary>Prevents duplicate processing of payment/refund operations.</summary>
public class IdempotencyKey
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty; // payment, refund, otp
    public string? ResponsePayload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
