namespace API.Domain.Entities;

/// <summary>
/// Represents the assignment of a crew member to a specific flight.
/// This is a junction entity for the many-to-many relationship between Flight and CrewMember.
/// </summary>
public class FlightCrew
{
    /// <summary>Unique identifier for the flight crew assignment.</summary>
    public int Id { get; set; }

    /// <summary>ID of the flight.</summary>
    public int FlightId { get; set; }

    /// <summary>ID of the crew member.</summary>
    public int CrewMemberId { get; set; }

    /// <summary>When the crew member was assigned to this flight.</summary>
    public DateTime AssignedAt { get; set; }

    // Navigation properties
    /// <summary>The flight this crew member is assigned to.</summary>
    public Flight? Flight { get; set; }

    /// <summary>The crew member assigned to this flight.</summary>
    public CrewMember? CrewMember { get; set; }
}
