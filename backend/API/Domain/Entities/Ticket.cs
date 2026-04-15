namespace API.Domain.Entities;

public class Ticket
{
    public int Id { get; set; }

    public int BookingPassengerId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public int Status { get; set; } = 0; // 0=Issued, 1=Used, 2=Refunded, 3=Cancelled

    public DateTime IssuedAt { get; set; }

    public int? ReplacedByTicketId { get; set; }

    // Navigation properties
    public virtual BookingPassenger BookingPassenger { get; set; } = null!;

    public virtual Ticket? ReplacedByTicket { get; set; }

    // Domain methods
    public bool IsValid() => Status == 0 && !ReplacedByTicketId.HasValue;

    public void MarkAsUsed()
    {
        Status = 1; // Used
    }

    public void ReplaceWith(int newTicketId)
    {
        ReplacedByTicketId = newTicketId;
        Status = 3; // Cancelled
    }
}
