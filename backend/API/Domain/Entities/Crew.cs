namespace API.Domain.Entities;

public class Crew
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FlightCrew> FlightCrews { get; set; } = [];
}
