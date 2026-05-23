namespace API.Domain.Entities;

public class FlightDisruptionDecision
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int AffectedFlightId { get; set; }
    public int LegType { get; set; } = 0; // 0=Outbound,1=Return
    public int Status { get; set; } = 0; // 0=Pending,1=Completed,2=Expired
    public int DecisionType { get; set; } = 0; // 0=None,1=Cancel,2=Rebook,3=AutoCancel
    public DateTime DecisionDeadline { get; set; }
    public DateTime? DecidedAt { get; set; }
    public int? NewFlightId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; } = 0;

    public virtual Booking Booking { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Flight AffectedFlight { get; set; } = null!;
    public virtual Flight? NewFlight { get; set; }
}
