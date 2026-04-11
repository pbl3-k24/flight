namespace FlightBooking.Domain.Entities;

/// <summary>Immutable accounting ledger for all monetary movements.</summary>
public class WalletLedger
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? RefundId { get; set; }
    public string EntryType { get; set; } = string.Empty; // credit, debit
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
