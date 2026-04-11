using Domain.Common;

namespace Domain.Entities.Infrastructure;

/// <summary>
/// Prevents duplicate operations (especially payment/refund).
/// Caller stores a UUID key; server rejects duplicate keys within the TTL.
/// </summary>
public class IdempotencyKey : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;   // e.g. "PAYMENT_CREATE"
    public string? ResponseBody { get; set; }
    public int ResponseStatusCode { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? UserId { get; set; }
}
