namespace API.Application.Services;

using API.Application.Dtos.FlightDefinition;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class FlightDefinitionService : IFlightDefinitionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FlightDefinitionService> _logger;

    public FlightDefinitionService(
        IUnitOfWork unitOfWork,
        ILogger<FlightDefinitionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<FlightDefinitionDto>> GetAllAsync()
    {
        var definitions = await _unitOfWork.FlightDefinitions.GetAllAsync();
        return definitions.Select(MapToDto).ToList();
    }

    public async Task<List<FlightDefinitionDto>> GetActiveAsync()
    {
        var definitions = await _unitOfWork.FlightDefinitions.GetActiveAsync();
        return definitions.Select(MapToDto).ToList();
    }

    public async Task<FlightDefinitionDto?> GetByIdAsync(int id)
    {
        var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(id);
        return definition == null ? null : MapToDto(definition);
    }

    public async Task<FlightDefinitionDto?> GetByFlightNumberAsync(string flightNumber)
    {
        var definition = await _unitOfWork.FlightDefinitions.GetByFlightNumberAsync(flightNumber);
        return definition == null ? null : MapToDto(definition);
    }

    public async Task<FlightDefinitionDto> CreateAsync(CreateFlightDefinitionDto dto)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(dto.FlightNumber))
        {
            throw new ValidationException("Flight number is required");
        }

        // Check duplicate flight number
        var existing = await _unitOfWork.FlightDefinitions.GetByFlightNumberAsync(dto.FlightNumber);
        if (existing != null)
        {
            throw new ValidationException($"Flight number {dto.FlightNumber} already exists");
        }

        // Validate route
        var route = await _unitOfWork.Routes.GetByIdAsync(dto.RouteId);
        if (route == null)
        {
            throw new NotFoundException($"Route {dto.RouteId} not found");
        }

        // Validate aircraft
        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(dto.DefaultAircraftId);
        if (aircraft == null)
        {
            throw new NotFoundException($"Aircraft {dto.DefaultAircraftId} not found");
        }

        var definition = new FlightDefinition
        {
            FlightNumber = dto.FlightNumber.ToUpper(),
            RouteId = dto.RouteId,
            DefaultAircraftId = dto.DefaultAircraftId,
            DepartureTime = dto.DepartureTime,
            ArrivalTime = dto.ArrivalTime,
            ArrivalOffsetDays = dto.ArrivalOffsetDays,
            OperatingDays = dto.OperatingDays,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _unitOfWork.FlightDefinitions.CreateAsync(definition);
        _logger.LogInformation("Created flight definition: {FlightNumber}", created.FlightNumber);

        return MapToDto(created);
    }

    public async Task<FlightDefinitionDto> UpdateAsync(int id, UpdateFlightDefinitionDto dto)
    {
        var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(id);
        if (definition == null)
        {
            throw new NotFoundException($"Flight definition {id} not found");
        }

        // Validate route
        var route = await _unitOfWork.Routes.GetByIdAsync(dto.RouteId);
        if (route == null)
        {
            throw new NotFoundException($"Route {dto.RouteId} not found");
        }

        // Validate aircraft
        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(dto.DefaultAircraftId);
        if (aircraft == null)
        {
            throw new NotFoundException($"Aircraft {dto.DefaultAircraftId} not found");
        }

        definition.RouteId = dto.RouteId;
        definition.DefaultAircraftId = dto.DefaultAircraftId;
        definition.DepartureTime = dto.DepartureTime;
        definition.ArrivalTime = dto.ArrivalTime;
        definition.ArrivalOffsetDays = dto.ArrivalOffsetDays;
        definition.OperatingDays = dto.OperatingDays;
        definition.IsActive = dto.IsActive;
        definition.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.FlightDefinitions.UpdateAsync(definition);
        _logger.LogInformation("Updated flight definition: {FlightNumber}", definition.FlightNumber);

        return MapToDto(definition);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(id);
        if (definition == null)
        {
            return false;
        }

        await _unitOfWork.FlightDefinitions.DeleteAsync(id);
        _logger.LogInformation("Deleted flight definition: {FlightNumber}", definition.FlightNumber);

        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(id);
        if (definition == null)
        {
            return false;
        }

        definition.Activate();
        await _unitOfWork.FlightDefinitions.UpdateAsync(definition);
        _logger.LogInformation("Activated flight definition: {FlightNumber}", definition.FlightNumber);

        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(id);
        if (definition == null)
        {
            return false;
        }

        definition.Deactivate();
        await _unitOfWork.FlightDefinitions.UpdateAsync(definition);
        _logger.LogInformation("Deactivated flight definition: {FlightNumber}", definition.FlightNumber);

        return true;
    }

    private FlightDefinitionDto MapToDto(FlightDefinition definition)
    {
        return new FlightDefinitionDto
        {
            Id = definition.Id,
            FlightNumber = definition.FlightNumber,
            RouteId = definition.RouteId,
            DefaultAircraftId = definition.DefaultAircraftId,
            DepartureTime = definition.DepartureTime,
            ArrivalTime = definition.ArrivalTime,
            ArrivalOffsetDays = definition.ArrivalOffsetDays,
            OperatingDays = definition.OperatingDays,
            IsActive = definition.IsActive,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt,
            RouteName = definition.Route != null 
                ? $"{definition.Route.DepartureAirport?.Code} → {definition.Route.ArrivalAirport?.Code}" 
                : null,
            DepartureAirportCode = definition.Route?.DepartureAirport?.Code,
            ArrivalAirportCode = definition.Route?.ArrivalAirport?.Code,
            AircraftModel = definition.DefaultAircraft?.Model,
            IsOvernightFlight = definition.IsOvernightFlight(),
            OperatingDaysText = GetOperatingDaysText(definition.OperatingDays)
        };
    }

    private string GetOperatingDaysText(int operatingDays)
    {
        if (operatingDays == 127) return "Every day";
        if (operatingDays == 31) return "Mon-Fri";
        if (operatingDays == 96) return "Sat-Sun";

        var days = new List<string>();
        if ((operatingDays & 1) != 0) days.Add("Mon");
        if ((operatingDays & 2) != 0) days.Add("Tue");
        if ((operatingDays & 4) != 0) days.Add("Wed");
        if ((operatingDays & 8) != 0) days.Add("Thu");
        if ((operatingDays & 16) != 0) days.Add("Fri");
        if ((operatingDays & 32) != 0) days.Add("Sat");
        if ((operatingDays & 64) != 0) days.Add("Sun");

        return string.Join(", ", days);
    }
}
