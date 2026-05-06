namespace API.Application.Interfaces;

using API.Application.Dtos.Admin;

public interface IFlightAdminService
{
    /// <summary>
    /// Creates a new flight.
    /// </summary>
    Task<FlightManagementResponse> CreateFlightAsync(CreateFlightDto dto);

    /// <summary>
    /// Creates flight schedule for one specific week and auto-generates the same pattern for following weeks.
    /// </summary>
    Task<List<FlightManagementResponse>> CreateWeeklyScheduleAsync(CreateWeeklyScheduleDto dto);

    /// <summary>
    /// Updates an existing flight.
    /// </summary>
    Task<bool> UpdateFlightAsync(int flightId, UpdateFlightDto dto);

    /// <summary>
    /// Deletes a flight (soft delete).
    /// </summary>
    Task<bool> DeleteFlightAsync(int flightId);

    /// <summary>
    /// Gets all flights with filters.
    /// </summary>
    Task<List<FlightManagementResponse>> GetFlightsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Creates a new route.
    /// </summary>
    Task<RouteManagementResponse> CreateRouteAsync(CreateRouteDto dto);

    /// <summary>
    /// Updates a route.
    /// </summary>
    Task<bool> UpdateRouteAsync(int routeId, UpdateRouteDto dto);

    /// <summary>
    /// Gets all routes.
    /// </summary>
    Task<List<RouteManagementResponse>> GetRoutesAsync(int page = 1, int pageSize = 20);
}
