namespace API.Domain.Entities;

public class Ticket
{
    public int Id { get; set; }

    public int BookingPassengerId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public int Status { get; set; } = 0; // 0=Issued, 1=Used, 2=Refunded, 3=Cancelled

    public DateTime IssuedAt { get; set; }

    public int? ReplacedByTicketId { get; set; }

    public int BookingId { get; set; }
    public int PassengerId { get; set; }
    public int FlightId { get; set; }
    public string? SeatNumber { get; set; }
    public int SeatClassId { get; set; }
    public decimal Price { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? BoardingTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual BookingPassenger BookingPassenger { get; set; } = null!;

    public virtual Ticket? ReplacedByTicket { get; set; }

    // Domain methods
    public bool IsValid() => Status == 0 && !ReplacedByTicketId.HasValue && !IsDeleted;

    public void MarkAsUsed()
    {
        Status = 1; // Used
    }

    public void ReplaceWith(int newTicketId)
    {
        ReplacedByTicketId = newTicketId;
        Status = 3; // Cancelled
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}
