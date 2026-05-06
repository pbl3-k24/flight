namespace API.Domain.Entities;

/// <summary>
/// Flight - Chuyến bay thực tế trong 1 ngày cụ thể
/// Tham chiếu đến FlightDefinition để lấy thông tin route, aircraft, giờ bay
/// </summary>
public class Flight
{
    public int Id { get; set; }

    // ===== REFERENCE TO DEFINITION =====
    
    /// <summary>
    /// Reference to flight definition (VJ123, VN201, etc.)
    /// This replaces RouteId - all route/aircraft/schedule info comes from FlightDefinition
    /// </summary>
    public int FlightDefinitionId { get; set; }

    // ===== ACTUAL FLIGHT DATA =====
    
    /// <summary>
    /// Actual departure date/time for this specific flight
    /// </summary>
    public DateTime DepartureTime { get; set; }

    /// <summary>
    /// Actual arrival date/time for this specific flight
    /// </summary>
    public DateTime ArrivalTime { get; set; }

    /// <summary>
    /// Actual aircraft used (can override FlightDefinition.DefaultAircraftId)
    /// </summary>
    public int? ActualAircraftId { get; set; }

    /// <summary>
    /// Flight status: 0=Scheduled, 1=Cancelled, 2=Delayed, 3=Completed, 4=InFlight
    /// </summary>
    public int Status { get; set; } = 0;

    // ===== AUDIT =====
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // ===== SOFT DELETE =====
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // ===== CONCURRENCY =====
    public int Version { get; set; } = 0;

    // ===== NAVIGATION =====
    public virtual FlightDefinition FlightDefinition { get; set; } = null!;
    public virtual Aircraft? ActualAircraft { get; set; }
    public virtual ICollection<FlightSeatInventory> SeatInventories { get; set; } = [];
    public virtual ICollection<Booking> OutboundBookings { get; set; } = [];
    public virtual ICollection<Booking> ReturnBookings { get; set; } = [];

    // ===== COMPUTED PROPERTIES =====
    
    /// <summary>
    /// Get flight number from definition
    /// </summary>
    public string FlightNumber => FlightDefinition?.FlightNumber ?? "UNKNOWN";

    /// <summary>
    /// Get route from definition
    /// </summary>
    public Route Route => FlightDefinition?.Route!;

    /// <summary>
    /// Get aircraft (actual or default from definition)
    /// </summary>
    public Aircraft Aircraft => ActualAircraft ?? FlightDefinition?.DefaultAircraft!;

    // ===== DOMAIN METHODS =====

    public Airport GetDepartureAirport() => FlightDefinition.Route.DepartureAirport;

    public Airport GetArrivalAirport() => FlightDefinition.Route.ArrivalAirport;

    public TimeSpan GetDuration() => ArrivalTime - DepartureTime;

    public bool CanBook(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(si => si.SeatClassId == seatClassId);
        return inventory?.CanHold(count) ?? false;
    }

    public void ReserveSeats(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(si => si.SeatClassId == seatClassId);
        if (inventory != null)
        {
            inventory.HoldSeats(count);
        }
    }

    public void ReleaseSeats(int seatClassId, int count)
    {
        var inventory = SeatInventories.FirstOrDefault(si => si.SeatClassId == seatClassId);
        if (inventory != null)
        {
            inventory.ReleaseHeldSeats(count);
        }
    }

    public void Cancel()
    {
        Status = 1; // Cancelled
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDepartureSoon(int hours)
    {
        var now = DateTime.UtcNow;
        var timeUntilDeparture = DepartureTime - now;
        return timeUntilDeparture.TotalHours <= hours && timeUntilDeparture.TotalHours > 0;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Override aircraft for this specific flight
    /// </summary>
    public void OverrideAircraft(int aircraftId)
    {
        ActualAircraftId = aircraftId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Use default aircraft from definition
    /// </summary>
    public void UseDefaultAircraft()
    {
        ActualAircraftId = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
