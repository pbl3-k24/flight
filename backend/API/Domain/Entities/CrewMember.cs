namespace API.Domain.Entities;

/// <summary>
/// Represents a crew member in the system.
/// </summary>
public class CrewMember
{
    /// <summary>Unique identifier for the crew member.</summary>
    public int Id { get; set; }

    /// <summary>First name of the crew member.</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>Last name of the crew member.</summary>
    public string LastName { get; set; } = null!;

    /// <summary>Role of the crew member (e.g., Pilot, Co-Pilot, Flight Attendant).</summary>
    public string Role { get; set; } = null!;

    /// <summary>License or certification number for the crew member.</summary>
    public string LicenseNumber { get; set; } = null!;

    /// <summary>When the crew member record was created in the system.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the crew member information was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    /// <summary>Flights this crew member is assigned to.</summary>
    public ICollection<FlightCrew> FlightAssignments { get; set; } = new List<FlightCrew>();
}
