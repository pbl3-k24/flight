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
    
    /// <summary>
    /// Route for this flight
    /// </summary>
    public int RouteId { get; set; }
    
    /// <summary>
    /// Aircraft assigned to this flight
    /// </summary>
    public int AircraftId { get; set; }
    
    /// <summary>
    /// Day of week (0 = Sunday, 1 = Monday, ..., 6 = Saturday)
    /// </summary>
    public int DayOfWeek { get; set; }
    
    /// <summary>
    /// Departure time (time only, no date)
    /// </summary>
    public TimeOnly DepartureTime { get; set; }
    
    /// <summary>
    /// Arrival time (time only, no date)
    /// If arrival < departure, it means next day
    /// </summary>
    public TimeOnly ArrivalTime { get; set; }
    
    /// <summary>
    /// Flight number prefix (e.g., "VJ" for VietJet, "VN" for Vietnam Airlines)
    /// Full flight number will be: Prefix + Number (e.g., VJ123)
    /// </summary>
    public string FlightNumberPrefix { get; set; } = null!;
    
    /// <summary>
    /// Flight number suffix (e.g., 123 for VJ123)
    /// </summary>
    public string FlightNumberSuffix { get; set; } = null!;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Navigation properties
    /// </summary>
    public virtual FlightScheduleTemplate Template { get; set; } = null!;
    public virtual Route Route { get; set; } = null!;
    public virtual Aircraft Aircraft { get; set; } = null!;
}
