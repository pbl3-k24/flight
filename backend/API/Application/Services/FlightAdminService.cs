namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class FlightAdminService : IFlightAdminService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IRouteRepository _routeRepository;
    private readonly IAirportRepository _airportRepository;
    private readonly IAircraftRepository _aircraftRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly ILogger<FlightAdminService> _logger;

    public FlightAdminService(
        IFlightRepository flightRepository,
        IRouteRepository routeRepository,
        IAirportRepository airportRepository,
        IAircraftRepository aircraftRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        ILogger<FlightAdminService> logger)
    {
        _flightRepository = flightRepository;
        _routeRepository = routeRepository;
        _airportRepository = airportRepository;
        _aircraftRepository = aircraftRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _logger = logger;
    }

    public async Task<FlightManagementResponse> CreateFlightAsync(CreateFlightDto dto)
    {
        try
        {
            // Validate route exists
            var route = await _routeRepository.GetByIdAsync(dto.RouteId);
            if (route == null)
            {
                throw new NotFoundException("Route not found");
            }

            // Validate aircraft exists
            var aircraft = await _aircraftRepository.GetByIdAsync(dto.AircraftId);
            if (aircraft == null)
            {
                throw new NotFoundException("Aircraft not found");
            }

            // Validate times
            if (dto.ArrivalTime <= dto.DepartureTime)
            {
                throw new ValidationException("Arrival time must be after departure time");
            }

            var flight = new Flight
            {
                FlightNumber = dto.FlightNumber,
                RouteId = dto.RouteId,
                AircraftId = dto.AircraftId,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                Status = dto.IsActive ? 0 : 1, // 0=Active, 1=Cancelled
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdFlight = await _flightRepository.CreateAsync(flight);

            _logger.LogInformation("Flight created: {FlightNumber}", dto.FlightNumber);

            return await BuildFlightResponseAsync(createdFlight);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flight");
            throw;
        }
    }

    public async Task<bool> UpdateFlightAsync(int flightId, UpdateFlightDto dto)
    {
        try
        {
            var flight = await _flightRepository.GetByIdAsync(flightId);
            if (flight == null)
            {
                throw new NotFoundException("Flight not found");
            }

            if (!string.IsNullOrEmpty(dto.FlightNumber))
            {
                flight.FlightNumber = dto.FlightNumber;
            }

            if (dto.AircraftId.HasValue)
            {
                flight.AircraftId = dto.AircraftId.Value;
            }

            if (dto.DepartureTime.HasValue)
            {
                flight.DepartureTime = dto.DepartureTime.Value;
            }

            if (dto.ArrivalTime.HasValue)
            {
                flight.ArrivalTime = dto.ArrivalTime.Value;
            }

            if (dto.IsActive.HasValue)
            {
                flight.Status = dto.IsActive.Value ? 0 : 1;
            }

            flight.UpdatedAt = DateTime.UtcNow;
            await _flightRepository.UpdateAsync(flight);

            _logger.LogInformation("Flight updated: {FlightId}", flightId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight");
            throw;
        }
    }

    public async Task<bool> DeleteFlightAsync(int flightId)
    {
        try
        {
            var flight = await _flightRepository.GetByIdAsync(flightId);
            if (flight == null)
            {
                throw new NotFoundException("Flight not found");
            }

            flight.Status = 1; // Cancelled (soft delete)
            flight.UpdatedAt = DateTime.UtcNow;
            await _flightRepository.UpdateAsync(flight);

            _logger.LogInformation("Flight deleted: {FlightId}", flightId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flight");
            throw;
        }
    }

    public async Task<List<FlightManagementResponse>> GetFlightsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var flights = await _flightRepository.GetAllAsync();
            var responses = new List<FlightManagementResponse>();

            foreach (var flight in flights.Skip((page - 1) * pageSize).Take(pageSize))
            {
                responses.Add(await BuildFlightResponseAsync(flight));
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flights");
            throw;
        }
    }

    public async Task<RouteManagementResponse> CreateRouteAsync(CreateRouteDto dto)
    {
        try
        {
            // Validate airports
            var departureAirport = await _airportRepository.GetByIdAsync(dto.DepartureAirportId);
            var arrivalAirport = await _airportRepository.GetByIdAsync(dto.ArrivalAirportId);

            if (departureAirport == null || arrivalAirport == null)
            {
                throw new NotFoundException("Airport not found");
            }

            if (dto.DepartureAirportId == dto.ArrivalAirportId)
            {
                throw new ValidationException("Departure and arrival airports must be different");
            }

            var route = new Route
            {
                DepartureAirportId = dto.DepartureAirportId,
                ArrivalAirportId = dto.ArrivalAirportId,
                DistanceKm = dto.DistanceKm,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                IsActive = true
            };

            var createdRoute = await _routeRepository.CreateAsync(route);

            _logger.LogInformation("Route created: {RouteId}", createdRoute.Id);

            return new RouteManagementResponse
            {
                RouteId = createdRoute.Id,
                DepartureAirport = departureAirport.Code,
                ArrivalAirport = arrivalAirport.Code,
                DistanceKm = createdRoute.DistanceKm,
                EstimatedDurationMinutes = createdRoute.EstimatedDurationMinutes,
                IsActive = createdRoute.IsActive
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating route");
            throw;
        }
    }

    public async Task<bool> UpdateRouteAsync(int routeId, UpdateRouteDto dto)
    {
        try
        {
            var route = await _routeRepository.GetByIdAsync(routeId);
            if (route == null)
            {
                throw new NotFoundException("Route not found");
            }

            if (dto.DistanceKm.HasValue)
            {
                route.DistanceKm = dto.DistanceKm.Value;
            }

            if (dto.EstimatedDurationMinutes.HasValue)
            {
                route.EstimatedDurationMinutes = dto.EstimatedDurationMinutes.Value;
            }

            await _routeRepository.UpdateAsync(route);

            _logger.LogInformation("Route updated: {RouteId}", routeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating route");
            throw;
        }
    }

    public async Task<List<RouteManagementResponse>> GetRoutesAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var routes = await _routeRepository.GetAllAsync();
            var responses = new List<RouteManagementResponse>();

            foreach (var route in routes.Skip((page - 1) * pageSize).Take(pageSize))
            {
                responses.Add(new RouteManagementResponse
                {
                    RouteId = route.Id,
                    DepartureAirport = route.DepartureAirport.Code,
                    ArrivalAirport = route.ArrivalAirport.Code,
                    DistanceKm = route.DistanceKm,
                    EstimatedDurationMinutes = route.EstimatedDurationMinutes,
                    IsActive = route.IsActive
                });
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting routes");
            throw;
        }
    }

    private async Task<FlightManagementResponse> BuildFlightResponseAsync(Flight flight)
    {
        var inventories = await _seatInventoryRepository.GetByFlightIdAsync(flight.Id);
        var totalSeats = inventories.Sum(i => i.TotalSeats);
        var bookedSeats = inventories.Sum(i => i.SoldSeats + i.HeldSeats);
        var availableSeats = totalSeats - bookedSeats;

        return new FlightManagementResponse
        {
            FlightId = flight.Id,
            FlightNumber = flight.FlightNumber,
            RouteCode = $"{flight.Route.DepartureAirport.Code}-{flight.Route.ArrivalAirport.Code}",
            AircraftModel = flight.Aircraft.Model,
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats,
            BookedSeats = bookedSeats,
            IsActive = flight.Status == 0,
            CreatedAt = flight.CreatedAt
        };
    }
}
