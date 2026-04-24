namespace API.Domain.Entities;

public class Route
{
    public int Id { get; set; }

    public int DepartureAirportId { get; set; }

    public int ArrivalAirportId { get; set; }

    public int DistanceKm { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Airport DepartureAirport { get; set; } = null!;

    public virtual Airport ArrivalAirport { get; set; } = null!;

    public virtual ICollection<Flight> Flights { get; set; } = [];

    // Domain methods
    public double GetDurationInHours() => EstimatedDurationMinutes / 60.0;

    public bool IsValid() => DepartureAirportId != ArrivalAirportId && !IsDeleted;

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
