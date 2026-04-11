namespace FlightBooking.Domain.Entities;

/// <summary>Transactional outbox pattern – guarantees events are published at least once.</summary>
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
