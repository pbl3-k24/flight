namespace API.Domain.Entities;

public class RefundRequest
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int PaymentId { get; set; }

    public decimal RefundAmount { get; set; }

    public string? Reason { get; set; }

    public int Status { get; set; } = 0; // 0=Pending, 1=Approved, 2=Processed, 3=Rejected

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;

    // Domain methods
    public void Approve()
    {
        Status = 1; // Approved
    }

    public void Process(string transactionId)
    {
        Status = 2; // Processed
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        Status = 3; // Rejected
        Reason = reason;
    }
}
