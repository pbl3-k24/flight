using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }

    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Method { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public int Status { get; set; }
    public string? TransactionRef { get; set; }
    public string? QrCodeData { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? RawCallbackData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public RefundRequest? RefundRequest { get; set; }

    public void MarkAsCompleted(string transactionRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionRef);
        Status = 1;
        TransactionRef = transactionRef;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = 2;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        Status = 3;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeRefunded() => Status == 1;
}
