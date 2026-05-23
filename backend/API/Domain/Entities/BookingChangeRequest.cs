namespace API.Domain.Entities;

public class BookingChangeRequest
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int LegType { get; set; } // 0=Outbound, 1=Return
    public int OldFlightId { get; set; }
    public int NewFlightId { get; set; }
    public decimal OldAmount { get; set; }
    public decimal NewAmount { get; set; }
    public decimal ChangeFee { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public int Status { get; set; } = 0; // 0=Pending,1=AwaitingPayment,2=Completed,3=Failed,4=Cancelled
    public int? PaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; } = 0;

    public virtual Booking Booking { get; set; } = null!;
    public virtual Flight OldFlight { get; set; } = null!;
    public virtual Flight NewFlight { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
}

