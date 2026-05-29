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
    /// Updates per-seat-class prices for a flight and writes audit logs.
    /// </summary>
    Task<bool> UpdateFlightPricesAsync(int flightId, UpdateFlightPricesDto dto, int? adminUserId);

    /// <summary>
    /// Deletes a flight (soft delete).
    /// </summary>
    Task<bool> DeleteFlightAsync(int flightId);

    /// <summary>
    /// Cancels a flight and all affected bookings, queues refunds for paid bookings,
    /// and sends cancellation notifications.
    /// </summary>
    Task<CancelFlightAdminResponse> CancelFlightAsync(int flightId, CancelFlightAdminDto dto);

    /// <summary>
    /// Gets all flights with filters.
    /// </summary>
    Task<List<FlightManagementResponse>> GetFlightsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets all flights for a specific Vietnam date.
    /// </summary>
    Task<List<FlightManagementResponse>> GetFlightsByDateAsync(DateOnly date);

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
