using API.Domain.Enums;

namespace API.Domain.Entities;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public PaymentMethod PaymentMethod { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? TransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public Booking? Booking { get; set; }

    public User? User { get; set; }

    public bool CanBeProcessed()
    {
        return Status == PaymentStatus.Pending && Amount > 0;
    }

    public void MarkProcessing(DateTime currentUtc)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only pending payments can move to processing.");
        }

        Status = PaymentStatus.Processing;
        ProcessedAt = currentUtc;
    }

    public void Complete(string transactionId, DateTime currentUtc)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
        {
            throw new InvalidOperationException("Payment is not in a completable state.");
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
        }

        Status = PaymentStatus.Completed;
        TransactionId = transactionId.Trim();
        ProcessedAt = currentUtc;
    }

    public void Fail(DateTime currentUtc)
    {
        if (Status is PaymentStatus.Completed or PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("Completed or refunded payments cannot be marked as failed.");
        }

        Status = PaymentStatus.Failed;
        ProcessedAt = currentUtc;
    }

    public void Refund(decimal refundAmount, DateTime currentUtc)
    {
        if (Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException("Only completed payments can be refunded.");
        }

        if (refundAmount <= 0 || refundAmount > Amount)
        {
            throw new ArgumentOutOfRangeException(nameof(refundAmount), "Refund amount must be > 0 and <= original amount.");
        }

        Status = PaymentStatus.Refunded;
        ProcessedAt = currentUtc;
    }

    public void ValidateInvariants()
    {
        if (Amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(Currency) || Currency.Length != 3)
        {
            throw new InvalidOperationException("Currency must be a 3-letter code.");
        }
    }
}
