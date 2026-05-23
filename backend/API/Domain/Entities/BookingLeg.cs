namespace API.Domain.Entities;

public class BookingLeg
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int FlightId { get; set; }
    public int SeatClassId { get; set; }
    public int LegType { get; set; } = 0; // 0=Outbound, 1=Return

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; } = 0;

    public virtual Booking Booking { get; set; } = null!;
    public virtual Flight Flight { get; set; } = null!;
    public virtual SeatClass SeatClass { get; set; } = null!;
    public virtual ICollection<BookingLegPassenger> LegPassengers { get; set; } = [];
}

