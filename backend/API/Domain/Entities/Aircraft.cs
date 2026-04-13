using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class Aircraft
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty;

    public int TotalSeats { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<AircraftSeatTemplate> SeatTemplates { get; set; } = new List<AircraftSeatTemplate>();
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();

    public int GetTotalSeatsByClass(SeatClass seatClass)
    {
        ArgumentNullException.ThrowIfNull(seatClass);
        return SeatTemplates.Where(x => x.SeatClassId == seatClass.Id).Sum(x => x.DefaultSeatCount);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
