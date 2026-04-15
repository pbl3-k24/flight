namespace API.Domain.Entities;

public class BookingService
{
    public int Id { get; set; }

    public int BookingPassengerId { get; set; }

    public string ServiceType { get; set; } = null!;

    public string ServiceName { get; set; } = null!;

    public decimal Price { get; set; }

    // Navigation properties
    public virtual BookingPassenger BookingPassenger { get; set; } = null!;
}
