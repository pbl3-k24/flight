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
    private readonly IUnitOfWork _unitOfWork;

    public FlightAdminServiceStub(
        ILogger<FlightAdminServiceStub> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
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

    public async Task<List<FlightManagementResponse>> GetFlightsAsync(int page, int pageSize)
    {
        _logger.LogInformation("Getting flights (page {Page}, pageSize {PageSize})", page, pageSize);
        
        try
        {
            // Get flights with related data
            var allFlights = await _unitOfWork.Flights.GetAllAsync();
            
            // Apply pagination
            var flights = allFlights
                .OrderByDescending(f => f.DepartureTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new List<FlightManagementResponse>();

            foreach (var flight in flights)
            {
                var flightDefinition = flight.FlightDefinition;
                var route = flightDefinition?.Route;
                var aircraft = flight.ActualAircraftId.HasValue 
                    ? await _unitOfWork.Aircraft.GetByIdAsync(flight.ActualAircraftId.Value)
                    : flightDefinition?.DefaultAircraft;

                // Calculate booked seats from FlightSeatInventory
                var seatInventories = await _unitOfWork.FlightSeatInventories.GetByFlightIdAsync(flight.Id);
                var totalSeats = seatInventories.Sum(s => s.TotalSeats);
                var availableSeats = seatInventories.Sum(s => s.AvailableSeats);
                var bookedSeats = totalSeats - availableSeats;

                response.Add(new FlightManagementResponse
                {
                    FlightId = flight.Id,
                    FlightNumber = flightDefinition?.FlightNumber ?? "N/A",
                    RouteCode = route != null 
                        ? $"{route.DepartureAirport?.Code ?? "?"} → {route.ArrivalAirport?.Code ?? "?"}"
                        : "N/A",
                    AircraftModel = aircraft?.Model ?? "N/A",
                    DepartureTime = flight.DepartureTime,
                    ArrivalTime = flight.ArrivalTime,
                    TotalSeats = totalSeats,
                    AvailableSeats = availableSeats,
                    BookedSeats = bookedSeats,
                    IsActive = flightDefinition?.IsActive ?? false,
                    CreatedAt = flight.CreatedAt
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flights");
            throw;
        }
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
