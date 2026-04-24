namespace API.Domain.Entities;

public class AircraftSeatTemplate
{
    public int Id { get; set; }

    public int AircraftId { get; set; }

    public int SeatClassId { get; set; }

    public int DefaultSeatCount { get; set; }

    public decimal DefaultBasePrice { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Aircraft Aircraft { get; set; } = null!;

    public virtual SeatClass SeatClass { get; set; } = null!;

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
