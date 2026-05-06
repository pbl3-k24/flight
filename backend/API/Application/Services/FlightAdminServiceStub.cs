namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// TEMPORARY STUB - FlightAdminService needs refactoring for FlightDefinition architecture
/// This stub prevents controller errors while migration is in progress
/// </summary>
public class FlightAdminServiceStub : IFlightAdminService
{
    private readonly ILogger<FlightAdminServiceStub> _logger;

    public FlightAdminServiceStub(ILogger<FlightAdminServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<FlightManagementResponse> CreateFlightAsync(CreateFlightDto dto)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled - use FlightDefinition APIs instead");
        throw new NotImplementedException("FlightAdminService is being refactored for FlightDefinition architecture. Use /api/v1/admin/flight-definitions instead.");
    }

    public Task<bool> UpdateFlightAsync(int flightId, UpdateFlightDto dto)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled");
        throw new NotImplementedException("FlightAdminService is being refactored for FlightDefinition architecture. Use /api/v1/admin/flight-definitions instead.");
    }

    public Task<bool> DeleteFlightAsync(int flightId)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled");
        throw new NotImplementedException("FlightAdminService is being refactored for FlightDefinition architecture.");
    }

    public Task<List<FlightManagementResponse>> GetFlightsAsync(int page, int pageSize)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled");
        throw new NotImplementedException("FlightAdminService is being refactored for FlightDefinition architecture. Use /api/v1/admin/flight-definitions instead.");
    }

    public Task<RouteManagementResponse> CreateRouteAsync(CreateRouteDto dto)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled");
        throw new NotImplementedException("Route management is being refactored.");
    }

    public Task<bool> UpdateRouteAsync(int routeId, UpdateRouteDto dto)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled");
        throw new NotImplementedException("Route management is being refactored.");
    }

    public Task<List<RouteManagementResponse>> GetRoutesAsync(int page, int pageSize)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled");
        throw new NotImplementedException("Route management is being refactored.");
    }

    public Task<List<FlightManagementResponse>> CreateWeeklyScheduleAsync(CreateWeeklyScheduleDto dto)
    {
        _logger.LogWarning("FlightAdminService is temporarily disabled");
        throw new NotImplementedException("Weekly schedule is being refactored. Use /api/v1/admin/flight-templates instead.");
    }
}
