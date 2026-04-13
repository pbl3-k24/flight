namespace API.Domain.Entities;

public class AircraftSeatTemplate
{
    public int Id { get; set; }
    public int AircraftId { get; set; }
    public int SeatClassId { get; set; }
    public int DefaultSeatCount { get; set; }
    public decimal DefaultBasePrice { get; set; }

    public Aircraft Aircraft { get; set; } = null!;
    public SeatClass SeatClass { get; set; } = null!;
}
