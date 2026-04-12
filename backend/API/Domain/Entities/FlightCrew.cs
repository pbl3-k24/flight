namespace API.Domain.Entities;

public class FlightCrew
{
    public int FlightId { get; set; }

    public int CrewId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Flight? Flight { get; set; }

    public Crew? Crew { get; set; }
}
