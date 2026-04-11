namespace FlightBooking.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty; // price_override, refund_approved, user_suspended, booking_cancelled
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Before { get; set; } // JSON snapshot
    public string? After { get; set; }  // JSON snapshot
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
