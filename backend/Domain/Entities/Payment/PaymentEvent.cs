using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Payment;

/// <summary>
/// Immutable event log for every payment gateway callback/event.
/// </summary>
public class PaymentEvent : BaseEntity
{
    public Guid PaymentId { get; set; }
    public PaymentEventType EventType { get; set; }
    public string RawPayload { get; set; } = string.Empty;  // Raw JSON from gateway
    public string? GatewaySignature { get; set; }

    // Navigation
    public Payment Payment { get; set; } = null!;
}
