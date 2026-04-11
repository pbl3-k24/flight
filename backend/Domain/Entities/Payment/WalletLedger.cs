using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Payment;

/// <summary>
/// Double-entry ledger. Every financial event is recorded and never updated.
/// </summary>
public class WalletLedger : BaseEntity
{
    public Guid? PaymentId { get; set; }
    public Guid? RefundId { get; set; }
    public Guid UserId { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
}
