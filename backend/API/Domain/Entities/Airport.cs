namespace API.Domain.Entities;

public class Airport
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Flight> DepartingFlights { get; set; } = [];

    public ICollection<Flight> ArrivingFlights { get; set; } = [];
}
