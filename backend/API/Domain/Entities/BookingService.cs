namespace API.Domain.Entities;

public class BookingService
{
    public int Id { get; set; }

    public int BookingPassengerId { get; set; }
    public int? BookingLegPassengerId { get; set; }

    public int AdditionalServiceId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual BookingPassenger BookingPassenger { get; set; } = null!;
    public virtual BookingLegPassenger? BookingLegPassenger { get; set; }

    public virtual AdditionalService AdditionalService { get; set; } = null!;

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
