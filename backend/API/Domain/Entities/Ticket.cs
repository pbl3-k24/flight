using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class Ticket
{
    public int Id { get; set; }
    public int BookingPassengerId { get; set; }

    [MaxLength(50)]
    public string TicketNumber { get; set; } = string.Empty;

    public int Status { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public int? ReplacedByTicketId { get; set; }

    public BookingPassenger BookingPassenger { get; set; } = null!;
    public Ticket? ReplacedByTicket { get; set; }

    public bool IsValid() => Status == 0;

    public void MarkAsUsed() => Status = 1;

    public void ReplaceWith(int newTicketId)
    {
        ReplacedByTicketId = newTicketId;
        Status = 3;
    }
}
