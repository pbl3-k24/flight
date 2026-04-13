using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class BookingService
{
    public int Id { get; set; }
    public int BookingPassengerId { get; set; }

    [MaxLength(50)]
    public string ServiceType { get; set; } = string.Empty;

    [MaxLength(255)]
    public string ServiceName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public BookingPassenger BookingPassenger { get; set; } = null!;
}
