namespace API.Domain.Entities;

/// <summary>
/// Template for flight schedules (e.g., "Summer Schedule", "Winter Schedule")
/// </summary>
public class FlightScheduleTemplate
{
    public int Id { get; set; }
    
    /// <summary>
    /// Template name (e.g., "Lịch bay mùa hè 2026")
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Description of the template
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this template is currently active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Navigation property to template details
    /// </summary>
    public virtual ICollection<FlightTemplateDetail> Details { get; set; } = new List<FlightTemplateDetail>();
}
