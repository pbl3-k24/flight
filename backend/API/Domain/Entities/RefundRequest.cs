using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class RefundRequest
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int PaymentId { get; set; }
    public decimal RefundAmount { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public Payment Payment { get; set; } = null!;

    public void Approve() => Status = 1;

    public void Process(string transactionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        Status = 2;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = 3;
        Reason = reason;
    }
}
