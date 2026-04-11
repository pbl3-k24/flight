using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class FlightService(IFlightRepository flightRepository, IAuditLogService auditLogService) : IFlightService
{
    public async Task<FlightDto> GetByIdAsync(Guid id)
    {
        var flight = await flightRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Flight {id} not found.");
        return MapToDto(flight);
    }

    public async Task<IEnumerable<FlightDto>> GetAllAsync(int page, int pageSize)
    {
        var flights = await flightRepository.GetAllAsync(page, pageSize);
        return flights.Select(MapToDto);
    }

    public async Task<FlightDto> CreateAsync(CreateFlightRequest request, Guid adminId)
    {
        var flight = new Flight
        {
            Id = Guid.NewGuid(),
            FlightNumber = request.FlightNumber,
            RouteId = request.RouteId,
            AircraftId = request.AircraftId,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            GateNumber = request.GateNumber,
            Status = "scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await flightRepository.AddAsync(flight);
        await flightRepository.SaveChangesAsync();
        await auditLogService.LogAsync("flight_created", "Flight", flight.Id.ToString(), null, new { flight.FlightNumber }, adminId);
        return MapToDto(flight);
    }

    public async Task<FlightDto> UpdateAsync(Guid id, UpdateFlightRequest request, Guid adminId)
    {
        var flight = await flightRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Flight {id} not found.");

        if (request.DepartureTime.HasValue) flight.DepartureTime = request.DepartureTime.Value;
        if (request.ArrivalTime.HasValue) flight.ArrivalTime = request.ArrivalTime.Value;
        if (request.GateNumber is not null) flight.GateNumber = request.GateNumber;
        if (request.Status is not null) flight.Status = request.Status;
        if (request.DelayReason is not null) flight.DelayReason = request.DelayReason;
        flight.UpdatedAt = DateTime.UtcNow;

        await flightRepository.SaveChangesAsync();
        return MapToDto(flight);
    }

    public async Task CancelFlightAsync(Guid id, string reason, Guid adminId)
    {
        var flight = await flightRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Flight {id} not found.");
        flight.Status = "cancelled";
        flight.UpdatedAt = DateTime.UtcNow;
        await flightRepository.SaveChangesAsync();
        await auditLogService.LogAsync("flight_cancelled", "Flight", id.ToString(), null, new { Reason = reason }, adminId);
    }

    public async Task UpdateStatusAsync(Guid id, string status, Guid adminId)
    {
        var flight = await flightRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Flight {id} not found.");
        flight.Status = status;
        flight.UpdatedAt = DateTime.UtcNow;
        await flightRepository.SaveChangesAsync();
        await auditLogService.LogAsync("flight_status_updated", "Flight", id.ToString(), null, new { Status = status }, adminId);
    }

    private static FlightDto MapToDto(Flight f) => new(
        f.Id, f.FlightNumber,
        f.Route is null ? new RouteDto(f.RouteId, "", "", "", "", true, true)
            : new RouteDto(f.Route.Id, f.Route.OriginAirport?.Code ?? "", f.Route.OriginAirport?.City ?? "",
                          f.Route.DestinationAirport?.Code ?? "", f.Route.DestinationAirport?.City ?? "",
                          f.Route.IsDomestic, f.Route.IsActive),
        f.Aircraft is null ? new AircraftDto(f.AircraftId, "", "", null, 0, true)
            : new AircraftDto(f.Aircraft.Id, f.Aircraft.Code, f.Aircraft.Model, f.Aircraft.Manufacturer, f.Aircraft.TotalSeats, f.Aircraft.IsActive),
        f.DepartureTime, f.ArrivalTime, f.Status, f.GateNumber,
        f.Inventories.Select(i => new FlightInventoryDto(i.FareClassId, i.FareClass?.Code ?? "", i.TotalSeats, i.AvailableSeats, i.HeldSeats)),
        f.FarePrices.Select(p => new FarePriceDto(p.FareClassId, p.FareClass?.Code ?? "", p.CurrentPrice, p.PriceSource)));
}

public class RouteService(IRouteRepository routeRepository) : IRouteService
{
    public async Task<RouteDto> GetByIdAsync(Guid id)
    {
        var route = await routeRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Route {id} not found.");
        return MapToDto(route);
    }

    public async Task<IEnumerable<RouteDto>> GetAllAsync()
    {
        var routes = await routeRepository.GetAllAsync();
        return routes.Select(MapToDto);
    }

    public async Task<RouteDto> CreateAsync(CreateRouteRequest request, Guid adminId)
    {
        var route = new Domain.Entities.Route
        {
            Id = Guid.NewGuid(),
            OriginAirportId = request.OriginAirportId,
            DestinationAirportId = request.DestinationAirportId,
            DistanceKm = request.DistanceKm,
            IsDomestic = true,
            IsActive = true
        };
        await routeRepository.AddAsync(route);
        await routeRepository.SaveChangesAsync();
        return MapToDto(route);
    }

    public async Task<RouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, Guid adminId)
    {
        var route = await routeRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Route {id} not found.");
        if (request.DistanceKm.HasValue) route.DistanceKm = request.DistanceKm;
        if (request.IsActive.HasValue) route.IsActive = request.IsActive.Value;
        await routeRepository.SaveChangesAsync();
        return MapToDto(route);
    }

    public async Task DeactivateAsync(Guid id, Guid adminId)
    {
        var route = await routeRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Route {id} not found.");
        route.IsActive = false;
        await routeRepository.SaveChangesAsync();
    }

    private static RouteDto MapToDto(Domain.Entities.Route r) =>
        new(r.Id, r.OriginAirport?.Code ?? "", r.OriginAirport?.City ?? "",
            r.DestinationAirport?.Code ?? "", r.DestinationAirport?.City ?? "",
            r.IsDomestic, r.IsActive);
}

public class AircraftService(IAircraftRepository aircraftRepository) : IAircraftService
{
    public async Task<AircraftDto> GetByIdAsync(Guid id)
    {
        var aircraft = await aircraftRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Aircraft {id} not found.");
        return MapToDto(aircraft);
    }

    public async Task<IEnumerable<AircraftDto>> GetAllAsync()
    {
        var aircrafts = await aircraftRepository.GetAllAsync();
        return aircrafts.Select(MapToDto);
    }

    public async Task<AircraftDto> CreateAsync(CreateAircraftRequest request, Guid adminId)
    {
        var aircraft = new Aircraft
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Model = request.Model,
            Manufacturer = request.Manufacturer,
            TotalSeats = request.TotalSeats,
            IsActive = true
        };
        await aircraftRepository.AddAsync(aircraft);
        await aircraftRepository.SaveChangesAsync();
        return MapToDto(aircraft);
    }

    public async Task<AircraftDto> UpdateAsync(Guid id, UpdateAircraftRequest request, Guid adminId)
    {
        var aircraft = await aircraftRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Aircraft {id} not found.");
        if (request.Model is not null) aircraft.Model = request.Model;
        if (request.Manufacturer is not null) aircraft.Manufacturer = request.Manufacturer;
        if (request.TotalSeats.HasValue) aircraft.TotalSeats = request.TotalSeats.Value;
        if (request.IsActive.HasValue) aircraft.IsActive = request.IsActive.Value;
        await aircraftRepository.SaveChangesAsync();
        return MapToDto(aircraft);
    }

    public async Task DeactivateAsync(Guid id, Guid adminId)
    {
        var aircraft = await aircraftRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Aircraft {id} not found.");
        aircraft.IsActive = false;
        await aircraftRepository.SaveChangesAsync();
    }

    private static AircraftDto MapToDto(Aircraft a) =>
        new(a.Id, a.Code, a.Model, a.Manufacturer, a.TotalSeats, a.IsActive);
}
