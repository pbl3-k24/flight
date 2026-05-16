namespace API.Domain.Entities;

/// <summary>
/// Detail of a flight schedule template - defines one flight in the weekly pattern
/// </summary>
public class FlightTemplateDetail
{
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the parent template
    /// </summary>
    public int TemplateId { get; set; }
    
    public int FlightDefinitionId { get; set; }
    
    /// <summary>
    /// Day of week (0 = Sunday, 1 = Monday, ..., 6 = Saturday)
    /// </summary>
    public int DayOfWeek { get; set; }
    
    public int? AircraftOverrideId { get; set; }

    public TimeOnly? DepartureTimeOverride { get; set; }

    public TimeOnly? ArrivalTimeOverride { get; set; }

    public int? ArrivalOffsetDaysOverride { get; set; }

    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Navigation properties
    /// </summary>
    public virtual FlightScheduleTemplate Template { get; set; } = null!;
    public virtual FlightDefinition FlightDefinition { get; set; } = null!;
    public virtual Aircraft? AircraftOverride { get; set; }
}
