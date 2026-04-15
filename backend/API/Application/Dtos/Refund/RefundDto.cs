namespace API.Application.Dtos.Refund;

public class RefundResponse
{
    public int RefundId { get; set; }

    public int BookingId { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!; // Pending, Approved, Processed, Rejected

    public DateTime RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public decimal RefundPercent { get; set; }

    public decimal PenaltyFee { get; set; }
}

public class RefundRequest
{
    public int BookingId { get; set; }

    public string Reason { get; set; } = null!;
}
