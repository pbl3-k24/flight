namespace API.Domain.Entities;

/// <summary>
/// Represents an airport in the flight system.
/// This is an aggregate root for airport-related operations.
/// </summary>
public class Airport
{
    /// <summary>Unique identifier for the airport.</summary>
    public int Id { get; set; }

    /// <summary>Airport's IATA code (e.g., LAX, JFK, ORD).</summary>
    public string Code { get; set; } = null!;

    /// <summary>Full name of the airport.</summary>
    public string Name { get; set; } = null!;

    /// <summary>City where the airport is located.</summary>
    public string City { get; set; } = null!;

    /// <summary>Country where the airport is located.</summary>
    public string Country { get; set; } = null!;

    /// <summary>Timezone of the airport (e.g., America/Los_Angeles).</summary>
    public string Timezone { get; set; } = null!;

    /// <summary>When the airport was created in the system.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the airport information was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>Flights departing from this airport.</summary>
    public ICollection<Flight> DepartingFlights { get; set; } = new List<Flight>();

    /// <summary>Flights arriving at this airport.</summary>
    public ICollection<Flight> ArrivingFlights { get; set; } = new List<Flight>();
}
