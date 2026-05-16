namespace API.Application.Services;

using API.Application.Dtos.FlightTemplate;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

public class FlightTemplateService : IFlightTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FlightTemplateService> _logger;
    private readonly int _defaultTurnaroundMinutes;

    public FlightTemplateService(
        IUnitOfWork unitOfWork,
        ILogger<FlightTemplateService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTurnaroundMinutes = configuration.GetValue("FlightScheduling:DefaultAircraftTurnaroundMinutes", 45);
    }

    public async Task<List<FlightScheduleTemplateDto>> GetAllTemplatesAsync()
    {
        var templates = await _unitOfWork.FlightScheduleTemplates.GetAllWithDetailsAsync();
        return templates.Select(MapToDto).ToList();
    }

    public async Task<FlightScheduleTemplateDto?> GetTemplateByIdAsync(int templateId)
    {
        var template = await _unitOfWork.FlightScheduleTemplates.GetByIdWithDetailsAsync(templateId);
        return template == null ? null : MapToDto(template);
    }

    public async Task<FlightScheduleTemplateDto> CreateTemplateAsync(CreateFlightTemplateDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ValidateTemplate(dto);

            foreach (var detail in dto.Details)
            {
                await ValidateFlightDetailAsync(detail);
            }

            ValidateDuplicateDetails(dto.Details);

            var template = new FlightScheduleTemplate
            {
                Code = dto.Code.Trim().ToUpperInvariant(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdTemplate = await _unitOfWork.FlightScheduleTemplates.CreateAsync(template);

            foreach (var detailDto in dto.Details)
            {
                var detail = new FlightTemplateDetail
                {
                    TemplateId = createdTemplate.Id,
                    FlightDefinitionId = detailDto.FlightDefinitionId,
                    DayOfWeek = detailDto.DayOfWeek,
                    AircraftOverrideId = detailDto.AircraftOverrideId,
                    DepartureTimeOverride = detailDto.DepartureTimeOverride,
                    ArrivalTimeOverride = detailDto.ArrivalTimeOverride,
                    ArrivalOffsetDaysOverride = detailDto.ArrivalOffsetDaysOverride,
                    IsActive = detailDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FlightTemplateDetails.CreateAsync(detail);
            }

            _logger.LogInformation("Created flight template: {TemplateCode} with {Count} details",
                template.Code, dto.Details.Count);

            var result = await _unitOfWork.FlightScheduleTemplates.GetByIdWithDetailsAsync(createdTemplate.Id);
            return MapToDto(result!);
        });
    }

    public async Task<FlightScheduleTemplateDto> UpdateTemplateAsync(int templateId, CreateFlightTemplateDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ValidateTemplate(dto);

            var template = await _unitOfWork.FlightScheduleTemplates.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new NotFoundException($"Template {templateId} not found");
            }

            foreach (var detail in dto.Details)
            {
                await ValidateFlightDetailAsync(detail);
            }

            ValidateDuplicateDetails(dto.Details);

            template.Code = dto.Code.Trim().ToUpperInvariant();
            template.Name = dto.Name.Trim();
            template.Description = dto.Description;
            template.EffectiveFrom = dto.EffectiveFrom;
            template.EffectiveTo = dto.EffectiveTo;
            template.IsActive = dto.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.FlightScheduleTemplates.UpdateAsync(template);
            await _unitOfWork.FlightTemplateDetails.DeleteByTemplateIdAsync(templateId);

            foreach (var detailDto in dto.Details)
            {
                var detail = new FlightTemplateDetail
                {
                    TemplateId = templateId,
                    FlightDefinitionId = detailDto.FlightDefinitionId,
                    DayOfWeek = detailDto.DayOfWeek,
                    AircraftOverrideId = detailDto.AircraftOverrideId,
                    DepartureTimeOverride = detailDto.DepartureTimeOverride,
                    ArrivalTimeOverride = detailDto.ArrivalTimeOverride,
                    ArrivalOffsetDaysOverride = detailDto.ArrivalOffsetDaysOverride,
                    IsActive = detailDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FlightTemplateDetails.CreateAsync(detail);
            }

            _logger.LogInformation("Updated flight template: {TemplateId}", templateId);

            var result = await _unitOfWork.FlightScheduleTemplates.GetByIdWithDetailsAsync(templateId);
            return MapToDto(result!);
        });
    }

    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        var template = await _unitOfWork.FlightScheduleTemplates.GetByIdAsync(templateId);
        if (template == null)
        {
            return false;
        }

        await _unitOfWork.FlightTemplateDetails.DeleteByTemplateIdAsync(templateId);
        await _unitOfWork.FlightScheduleTemplates.DeleteAsync(templateId);

        _logger.LogInformation("Deleted flight template: {TemplateId}", templateId);
        return true;
    }

    public async Task<GenerateFlightsResultDto> GenerateFlightsFromTemplateAsync(GenerateFlightsFromTemplateDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (dto.TemplateId <= 0)
            {
                throw new ValidationException("TemplateId must be greater than 0");
            }

            if (dto.NumberOfWeeks < 1 || dto.NumberOfWeeks > 52)
            {
                throw new ValidationException("Number of weeks must be between 1 and 52");
            }

            var template = await _unitOfWork.FlightScheduleTemplates.GetByIdWithDetailsAsync(dto.TemplateId);
            if (template == null)
            {
                throw new NotFoundException($"Template {dto.TemplateId} not found");
            }

            if (!template.IsActive)
            {
                throw new ValidationException($"Template {dto.TemplateId} is not active");
            }

            if (template.Details == null || !template.Details.Any(d => d.IsActive))
            {
                throw new ValidationException($"Template {dto.TemplateId} has no active flight details");
            }

            var result = new GenerateFlightsResultDto();

            for (var week = 0; week < dto.NumberOfWeeks; week++)
            {
                var weekStart = dto.WeekStartDate.Date.AddDays(week * 7);
                await GenerateFlightsForWeekAsync(template, weekStart, result);
            }

            _logger.LogInformation("Flight generation completed: {Generated} generated, {Skipped} skipped",
                result.TotalFlightsGenerated, result.TotalFlightsSkipped);

            return result;
        });
    }

    private async Task GenerateFlightsForWeekAsync(
        FlightScheduleTemplate template,
        DateTime weekStart,
        GenerateFlightsResultDto result)
    {
        foreach (var detail in template.Details.Where(d => d.IsActive))
        {
            var definition = detail.FlightDefinition;
            if (definition == null)
            {
                throw new ValidationException($"Template detail {detail.Id} has no flight definition loaded");
            }

            if (!definition.IsActive)
            {
                result.TotalFlightsSkipped++;
                result.Warnings.Add($"Skipped inactive flight definition {definition.FlightNumber}");
                continue;
            }

            var flightDate = weekStart.AddDays(detail.DayOfWeek).Date;
            if (template.EffectiveFrom.HasValue && DateOnly.FromDateTime(flightDate) < template.EffectiveFrom.Value)
            {
                result.TotalFlightsSkipped++;
                continue;
            }

            if (template.EffectiveTo.HasValue && DateOnly.FromDateTime(flightDate) > template.EffectiveTo.Value)
            {
                result.TotalFlightsSkipped++;
                continue;
            }

            var departureTime = detail.DepartureTimeOverride ?? definition.DepartureTime;
            var arrivalTime = detail.ArrivalTimeOverride ?? definition.ArrivalTime;
            var arrivalOffsetDays = detail.ArrivalOffsetDaysOverride ?? definition.ArrivalOffsetDays;
            ValidateSchedule(departureTime, arrivalTime, arrivalOffsetDays);

            var departureDateTime = flightDate.Add(departureTime.ToTimeSpan());
            var arrivalDateTime = flightDate.AddDays(arrivalOffsetDays).Add(arrivalTime.ToTimeSpan());
            var aircraftId = detail.AircraftOverrideId ?? definition.DefaultAircraftId;
            await _unitOfWork.Flights.AcquireAircraftGenerationLockAsync(aircraftId);

            if (await IsFlightDuplicateAsync(definition.Id, departureDateTime))
            {
                result.TotalFlightsSkipped++;
                result.Warnings.Add($"Skipped duplicate flight {definition.FlightNumber} at {departureDateTime:yyyy-MM-dd HH:mm}");
                continue;
            }

            if (await HasAircraftConflictAsync(aircraftId, departureDateTime, arrivalDateTime))
            {
                throw new ValidationException(
                    $"Aircraft {aircraftId} conflicts with an existing flight around {departureDateTime:yyyy-MM-dd HH:mm}. " +
                    $"Turnaround timeout: {_defaultTurnaroundMinutes} minutes.");
            }

            var flight = new Flight
            {
                FlightDefinitionId = definition.Id,
                FlightNumber = definition.FlightNumber,
                RouteId = definition.RouteId,
                AircraftId = aircraftId,
                ActualAircraftId = null,
                DepartureTime = departureDateTime,
                ArrivalTime = arrivalDateTime,
                ArrivalOffsetDays = arrivalOffsetDays,
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.Flights.CreateAsync(flight);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                result.TotalFlightsSkipped++;
                result.Warnings.Add($"Skipped duplicate flight {definition.FlightNumber} at {departureDateTime:yyyy-MM-dd HH:mm}");
                continue;
            }

            await CreateSeatInventoryForFlightAsync(flight, aircraftId);

            result.TotalFlightsGenerated++;

            _logger.LogDebug("Generated flight {FlightNumber} on {Date}", definition.FlightNumber, flightDate);
        }
    }

    private async Task<bool> IsFlightDuplicateAsync(int flightDefinitionId, DateTime departureDateTime)
    {
        return await _unitOfWork.Flights.ExistsByDefinitionAndDepartureAsync(flightDefinitionId, departureDateTime);
    }

    private async Task<bool> HasAircraftConflictAsync(int aircraftId, DateTime newDeparture, DateTime newArrival)
    {
        return await _unitOfWork.Flights.HasAircraftConflictAsync(
            aircraftId,
            newDeparture,
            newArrival,
            _defaultTurnaroundMinutes);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pgEx &&
               pgEx.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private async Task CreateSeatInventoryForFlightAsync(Flight flight, int aircraftId)
    {
        var aircraft = await _unitOfWork.Aircraft.GetByIdWithSeatTemplatesAsync(aircraftId);
        if (aircraft == null)
        {
            throw new NotFoundException($"Aircraft {aircraftId} not found");
        }

        var activeTemplates = aircraft.SeatTemplates.Where(t => !t.IsDeleted).ToList();
        if (!activeTemplates.Any())
        {
            throw new ValidationException($"Aircraft {aircraftId} has no active seat templates");
        }

        foreach (var template in activeTemplates)
        {
            var inventory = new FlightSeatInventory
            {
                FlightId = flight.Id,
                SeatClassId = template.SeatClassId,
                TotalSeats = template.DefaultSeatCount,
                AvailableSeats = template.DefaultSeatCount,
                HeldSeats = 0,
                SoldSeats = 0,
                BasePrice = template.DefaultBasePrice,
                CurrentPrice = template.DefaultBasePrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FlightSeatInventories.CreateAsync(inventory);
        }
    }

    private void ValidateTemplate(CreateFlightTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            throw new ValidationException("Template code is required");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationException("Template name is required");
        }

        if (dto.EffectiveFrom.HasValue && dto.EffectiveTo.HasValue && dto.EffectiveFrom.Value > dto.EffectiveTo.Value)
        {
            throw new ValidationException("EffectiveFrom must be before or equal to EffectiveTo");
        }

        if (dto.Details == null || dto.Details.Count == 0)
        {
            throw new ValidationException("Template must have at least one flight detail");
        }
    }

    private async Task ValidateFlightDetailAsync(CreateFlightTemplateDetailDto detail)
    {
        if (detail.DayOfWeek < 0 || detail.DayOfWeek > 6)
        {
            throw new ValidationException("DayOfWeek must be between 0 and 6");
        }

        var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(detail.FlightDefinitionId);
        if (definition == null)
        {
            throw new NotFoundException($"Flight definition {detail.FlightDefinitionId} not found");
        }

        if (detail.AircraftOverrideId.HasValue)
        {
            var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(detail.AircraftOverrideId.Value);
            if (aircraft == null)
            {
                throw new NotFoundException($"Aircraft override {detail.AircraftOverrideId.Value} not found");
            }
        }

        var departure = detail.DepartureTimeOverride ?? definition.DepartureTime;
        var arrival = detail.ArrivalTimeOverride ?? definition.ArrivalTime;
        var offsetDays = detail.ArrivalOffsetDaysOverride ?? definition.ArrivalOffsetDays;
        ValidateSchedule(departure, arrival, offsetDays);
    }

    private void ValidateDuplicateDetails(List<CreateFlightTemplateDetailDto> details)
    {
        var duplicate = details
            .GroupBy(d => new
            {
                d.FlightDefinitionId,
                d.DayOfWeek,
                Departure = d.DepartureTimeOverride
            })
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new ValidationException(
                $"Duplicate weekly template detail for FlightDefinitionId={duplicate.Key.FlightDefinitionId}, DayOfWeek={duplicate.Key.DayOfWeek}");
        }
    }

    private void ValidateSchedule(TimeOnly departureTime, TimeOnly arrivalTime, int arrivalOffsetDays)
    {
        if (arrivalOffsetDays < 0 || arrivalOffsetDays > 2)
        {
            throw new ValidationException("ArrivalOffsetDays must be between 0 and 2");
        }

        if (arrivalTime < departureTime && arrivalOffsetDays == 0)
        {
            throw new ValidationException("ArrivalOffsetDays must be greater than 0 when arrival time is earlier than departure time");
        }

        if (arrivalTime == departureTime && arrivalOffsetDays == 0)
        {
            throw new ValidationException("Arrival time must be different from departure time unless ArrivalOffsetDays is greater than 0");
        }
    }

    private FlightScheduleTemplateDto MapToDto(FlightScheduleTemplate template)
    {
        return new FlightScheduleTemplateDto
        {
            Id = template.Id,
            Code = template.Code,
            Name = template.Name,
            Description = template.Description,
            EffectiveFrom = template.EffectiveFrom,
            EffectiveTo = template.EffectiveTo,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Details = template.Details?.Select(MapDetailToDto).ToList() ?? new List<FlightTemplateDetailDto>()
        };
    }

    private FlightTemplateDetailDto MapDetailToDto(FlightTemplateDetail detail)
    {
        var definition = detail.FlightDefinition;
        var departureTime = detail.DepartureTimeOverride ?? definition.DepartureTime;
        var arrivalTime = detail.ArrivalTimeOverride ?? definition.ArrivalTime;
        var arrivalOffsetDays = detail.ArrivalOffsetDaysOverride ?? definition.ArrivalOffsetDays;
        var aircraft = detail.AircraftOverride ?? definition.DefaultAircraft;

        return new FlightTemplateDetailDto
        {
            Id = detail.Id,
            TemplateId = detail.TemplateId,
            FlightDefinitionId = detail.FlightDefinitionId,
            FlightNumber = definition.FlightNumber,
            RouteId = definition.RouteId,
            AircraftId = aircraft.Id,
            AircraftOverrideId = detail.AircraftOverrideId,
            DayOfWeek = detail.DayOfWeek,
            DayOfWeekName = GetDayOfWeekName(detail.DayOfWeek),
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            ArrivalOffsetDays = arrivalOffsetDays,
            DepartureTimeOverride = detail.DepartureTimeOverride,
            ArrivalTimeOverride = detail.ArrivalTimeOverride,
            ArrivalOffsetDaysOverride = detail.ArrivalOffsetDaysOverride,
            IsActive = detail.IsActive,
            RouteName = definition.Route != null
                ? $"{definition.Route.DepartureAirport?.Code} -> {definition.Route.ArrivalAirport?.Code}"
                : null,
            AircraftName = aircraft.Model
        };
    }

    private string GetDayOfWeekName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            _ => "Unknown"
        };
    }
}
