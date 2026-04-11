using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Payment;

public class Refund : BaseEntity
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? GatewayRefundRef { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public Payment Payment { get; set; } = null!;
}
