namespace API.Domain.Entities;

public class AircraftSeatTemplate
{
    public int Id { get; set; }

    public int AircraftId { get; set; }

    public int SeatClassId { get; set; }

    public int DefaultSeatCount { get; set; }

    public decimal DefaultBasePrice { get; set; }

    // Navigation properties
    public virtual Aircraft Aircraft { get; set; } = null!;

    public virtual SeatClass SeatClass { get; set; } = null!;
}
