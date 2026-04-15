namespace API.Domain.Entities;

public class Aircraft
{
    public int Id { get; set; }

    public string Model { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    public int TotalSeats { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<AircraftSeatTemplate> SeatTemplates { get; set; } = [];

    public virtual ICollection<Flight> Flights { get; set; } = [];

    // Domain methods
    public int GetTotalSeatsByClass(SeatClass seatClass)
    {
        return SeatTemplates
            .Where(st => st.SeatClass?.Id == seatClass.Id)
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
}
