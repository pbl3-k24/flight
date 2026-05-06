namespace API.Domain.Entities;

public class Aircraft
{
    public int Id { get; set; }

    public string Model { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    public int TotalSeats { get; set; }

    public bool IsActive { get; set; } = true;

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual ICollection<AircraftSeatTemplate> SeatTemplates { get; set; } = [];

    // NOTE: Flights now reference Aircraft through FlightDefinition.DefaultAircraftId or Flight.ActualAircraftId
    // This navigation is no longer used
    // public virtual ICollection<Flight> Flights { get; set; } = [];

    // Domain methods
    public int GetTotalSeatsByClass(SeatClass seatClass)
    {
        return SeatTemplates
            .Where(st => st.SeatClass?.Id == seatClass.Id && !st.IsDeleted)
            .Sum(st => st.DefaultSeatCount);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

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
