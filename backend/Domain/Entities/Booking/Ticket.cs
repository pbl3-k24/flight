using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Booking;

public class Ticket : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;  // e.g. VN-TKT-20240101-000001
    public Guid BookingItemId { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Issued;
    public DateTime IssuedAt { get; set; }
    public string? BarcodeData { get; set; }   // QR / barcode content

    // Navigation
    public BookingItem BookingItem { get; set; } = null!;
}
