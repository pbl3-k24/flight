namespace API.Domain.Entities;

public class BookingService
{
    public int Id { get; set; }

    public int BookingPassengerId { get; set; }

    public string ServiceType { get; set; } = null!;

    public string ServiceName { get; set; } = null!;

    public decimal Price { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual BookingPassenger BookingPassenger { get; set; } = null!;

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
