using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Infrastructure;

/// <summary>
/// Immutable audit trail for any significant action in the system.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;     // e.g. "BOOKING_CREATED"
    public string EntityName { get; set; } = string.Empty; // e.g. "Booking"
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }                 // JSON snapshot
    public string? NewValues { get; set; }                 // JSON snapshot
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
