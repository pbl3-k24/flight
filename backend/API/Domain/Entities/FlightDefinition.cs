namespace API.Domain.Entities;

/// <summary>
/// Flight Definition - Định nghĩa chuyến bay cố định (VD: VJ123, VN201)
/// Đây là "template" cho các chuyến bay thực tế
/// </summary>
public class FlightDefinition
{
    public int Id { get; set; }

    // ===== BUSINESS DATA =====
    
    /// <summary>
    /// Flight number (VD: VJ123, VN201) - UNIQUE
    /// </summary>
    public string FlightNumber { get; set; } = null!;

    /// <summary>
    /// Route for this flight definition
    /// </summary>
    public int RouteId { get; set; }

    /// <summary>
    /// Default aircraft for this flight
    /// </summary>
    public int DefaultAircraftId { get; set; }

    /// <summary>
    /// Departure time (time only, no date)
    /// </summary>
    public TimeOnly DepartureTime { get; set; }

    /// <summary>
    /// Arrival time (time only, no date)
    /// </summary>
    public TimeOnly ArrivalTime { get; set; }

    /// <summary>
    /// Arrival offset days for overnight flights
    /// 0 = same day, 1 = next day, 2 = 2 days later
    /// </summary>
    public int ArrivalOffsetDays { get; set; } = 0;

    /// <summary>
    /// Days of week this flight operates (bit flags)
    /// 1 = Monday, 2 = Tuesday, 4 = Wednesday, 8 = Thursday, 16 = Friday, 32 = Saturday, 64 = Sunday
    /// Example: 31 = Mon-Fri, 127 = Every day
    /// </summary>
    public int OperatingDays { get; set; } = 127; // Default: every day

    /// <summary>
    /// Is this flight definition active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    // ===== AUDIT =====
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ===== NAVIGATION =====
    public virtual Route Route { get; set; } = null!;
    public virtual Aircraft DefaultAircraft { get; set; } = null!;
    
    /// <summary>
    /// All actual flights generated from this definition
    /// </summary>
    public virtual ICollection<Flight> Flights { get; set; } = [];

    // ===== DOMAIN METHODS =====

    /// <summary>
    /// Check if this is an overnight flight
    /// </summary>
    public bool IsOvernightFlight()
    {
        return ArrivalOffsetDays > 0 || ArrivalTime < DepartureTime;
    }

    /// <summary>
    /// Get flight duration
    /// </summary>
    public TimeSpan GetDuration()
    {
        var departure = DepartureTime.ToTimeSpan();
        var arrival = ArrivalTime.ToTimeSpan();

        if (IsOvernightFlight())
        {
            arrival += TimeSpan.FromDays(ArrivalOffsetDays == 0 ? 1 : ArrivalOffsetDays);
        }

        return arrival - departure;
    }

    /// <summary>
    /// Check if flight operates on a specific day of week
    /// </summary>
    public bool OperatesOnDay(DayOfWeek dayOfWeek)
    {
        int dayFlag = 1 << (int)dayOfWeek;
        return (OperatingDays & dayFlag) != 0;
    }

    /// <summary>
    /// Validate flight definition
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FlightNumber)
            && RouteId > 0
            && DefaultAircraftId > 0
            && DepartureTime != ArrivalTime
            && OperatingDays > 0;
    }

    /// <summary>
    /// Update schedule
    /// </summary>
    public void UpdateSchedule(TimeOnly departure, TimeOnly arrival, int offsetDays = 0)
    {
        DepartureTime = departure;
        ArrivalTime = arrival;
        ArrivalOffsetDays = offsetDays;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update operating days
    /// </summary>
    public void SetOperatingDays(params DayOfWeek[] days)
    {
        OperatingDays = 0;
        foreach (var day in days)
        {
            OperatingDays |= (1 << (int)day);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate this flight definition
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate this flight definition
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Change default aircraft
    /// </summary>
    public void ChangeAircraft(int newAircraftId)
    {
        DefaultAircraftId = newAircraftId;
        UpdatedAt = DateTime.UtcNow;
    }
}
