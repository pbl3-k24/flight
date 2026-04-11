using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Infrastructure;

/// <summary>
/// Transactional outbox pattern: events written in the same DB transaction
/// as the business operation are picked up by a relay and published to the
/// message broker, guaranteeing at-least-once delivery.
/// </summary>
public class OutboxEvent : BaseEntity
{
    public string EventType { get; set; } = string.Empty;   // e.g. "BookingConfirmed"
    public string Payload { get; set; } = "{}";             // JSON
    public OutboxEventStatus Status { get; set; } = OutboxEventStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? AggregateId { get; set; }
    public string? AggregateName { get; set; }
}
