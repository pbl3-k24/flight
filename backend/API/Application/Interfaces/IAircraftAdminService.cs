namespace API.Application.Interfaces;

using API.Application.Dtos.Admin;

/// <summary>
/// Service for managing aircraft (Admin only)
/// </summary>
public interface IAircraftAdminService
{
    /// <summary>
    /// Get all aircraft with pagination
    /// </summary>
    Task<List<AircraftManagementResponse>> GetAllAircraftAsync(int page, int pageSize, bool includeDeleted = false);

    /// <summary>
    /// Get aircraft by ID
    /// </summary>
    Task<AircraftManagementResponse?> GetAircraftByIdAsync(int aircraftId);

    /// <summary>
    /// Create new aircraft with seat configuration
    /// </summary>
    Task<AircraftManagementResponse> CreateAircraftAsync(CreateAircraftDto dto);

    /// <summary>
    /// Update aircraft information
    /// </summary>
    Task<AircraftManagementResponse> UpdateAircraftAsync(int aircraftId, UpdateAircraftDto dto);

    /// <summary>
    /// Soft delete aircraft (can only delete if no active flights)
    /// </summary>
    Task<bool> DeleteAircraftAsync(int aircraftId);

    /// <summary>
    /// Restore soft-deleted aircraft
    /// </summary>
    Task<bool> RestoreAircraftAsync(int aircraftId);

    /// <summary>
    /// Get aircraft statistics
    /// </summary>
    Task<AircraftStatisticsResponse> GetAircraftStatisticsAsync(int aircraftId);
}

public class AircraftStatisticsResponse
{
    public int AircraftId { get; set; }
    public string RegistrationNumber { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int TotalFlights { get; set; }
    public int ActiveFlights { get; set; }
    public int CompletedFlights { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageOccupancyRate { get; set; }
}
