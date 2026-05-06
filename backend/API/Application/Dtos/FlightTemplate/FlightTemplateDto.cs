namespace API.Application.Dtos.FlightTemplate;

using System.ComponentModel.DataAnnotations;

public class FlightScheduleTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<FlightTemplateDetailDto> Details { get; set; } = new();
}

public class FlightTemplateDetailDto
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int RouteId { get; set; }
    public int AircraftId { get; set; }
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = null!; // "Monday", "Tuesday", etc.
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public string FlightNumber { get; set; } = null!; // Prefix + Suffix
    public string? RouteName { get; set; } // "SGN → HAN"
    public string? AircraftName { get; set; } // "Boeing 787"
}

public class CreateFlightTemplateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateFlightTemplateDetailDto> Details { get; set; } = new();
}

public class CreateFlightTemplateDetailDto
{
    public int RouteId { get; set; }
    public int AircraftId { get; set; }
    public int DayOfWeek { get; set; } // 0-6
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public string FlightNumberPrefix { get; set; } = null!;
    public string FlightNumberSuffix { get; set; } = null!;
}

public class GenerateFlightsFromTemplateDto
{
    /// <summary>
    /// Start date of the week (Monday recommended)
    /// </summary>
    [Required(ErrorMessage = "WeekStartDate is required")]
    public DateTime WeekStartDate { get; set; }
    
    /// <summary>
    /// Template ID to use
    /// </summary>
    [Required(ErrorMessage = "TemplateId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "TemplateId must be greater than 0")]
    public int TemplateId { get; set; }
    
    /// <summary>
    /// Number of weeks to generate (default: 1)
    /// </summary>
    [Range(1, 52, ErrorMessage = "NumberOfWeeks must be between 1 and 52")]
    public int NumberOfWeeks { get; set; } = 1;
}

public class GenerateFlightsResultDto
{
    public int TotalFlightsGenerated { get; set; }
    public int TotalFlightsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
