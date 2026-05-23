namespace API.Domain.Entities;

public class TicketUpgradeRequest
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int TicketId { get; set; }
    public int BookingPassengerId { get; set; }
    public int FlightId { get; set; }
    public int FromSeatClassId { get; set; }
    public int ToSeatClassId { get; set; }
    public int FromInventoryId { get; set; }
    public int ToInventoryId { get; set; }
    public decimal PriceDifference { get; set; }
    public string Currency { get; set; } = "VND";
    public int Status { get; set; } = (int)TicketUpgradeStatus.Pending;
    public int? PaymentId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; }

    public virtual Booking Booking { get; set; } = null!;
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
}

public enum TicketUpgradeStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2,
    Expired = 3,
    Failed = 4
}
