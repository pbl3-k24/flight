namespace API.Domain.Entities;

public class BookingLegPassenger
{
    public int Id { get; set; }
    public int BookingLegId { get; set; }
    public int BookingPassengerId { get; set; }
    public int FlightSeatInventoryId { get; set; }
    public int DocumentCheckStatus { get; set; } = (int)PassengerDocumentCheckStatus.Pending;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; } = 0;

    public virtual BookingLeg BookingLeg { get; set; } = null!;
    public virtual BookingPassenger BookingPassenger { get; set; } = null!;
    public virtual FlightSeatInventory FlightSeatInventory { get; set; } = null!;
    public virtual ICollection<BookingService> Services { get; set; } = [];
}

