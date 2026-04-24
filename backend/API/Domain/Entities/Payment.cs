namespace API.Domain.Entities;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string Provider { get; set; } = null!;

    public string Method { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "VND";

    public int Status { get; set; } = 0; // 0=Pending, 1=Completed, 2=Failed, 3=Refunded

    public string? TransactionRef { get; set; }

    public string? QrCodeData { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? RawCallbackData { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Audit properties
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Concurrency token
    public int Version { get; set; } = 0;

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;

    public virtual RefundRequest? RefundRequest { get; set; }

    // Domain methods
    public void MarkAsCompleted(string transactionRef)
    {
        Status = 1; // Completed
        TransactionRef = transactionRef;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = 2; // Failed
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        Status = 3; // Refunded
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeRefunded() => Status == 1; // Only completed payments can be refunded

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
