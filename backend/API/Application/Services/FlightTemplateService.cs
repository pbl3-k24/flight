namespace API.Application.Services;

using API.Application.Dtos.FlightTemplate;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class FlightTemplateService : IFlightTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FlightTemplateService> _logger;

    public FlightTemplateService(
        IUnitOfWork unitOfWork,
        ILogger<FlightTemplateService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ========== CRUD Operations ==========

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
        // Validate
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationException("Template name is required");
        }

        if (dto.Details == null || dto.Details.Count == 0)
        {
            throw new ValidationException("Template must have at least one flight detail");
        }

        // Validate each detail
        foreach (var detail in dto.Details)
        {
            await ValidateFlightDetailAsync(detail);
        }

        var template = new FlightScheduleTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create template first
        var createdTemplate = await _unitOfWork.FlightScheduleTemplates.CreateAsync(template);

        // Then create details
        foreach (var detailDto in dto.Details)
        {
            var detail = new FlightTemplateDetail
            {
                TemplateId = createdTemplate.Id,
                RouteId = detailDto.RouteId,
                AircraftId = detailDto.AircraftId,
                DayOfWeek = detailDto.DayOfWeek,
                DepartureTime = detailDto.DepartureTime,
                ArrivalTime = detailDto.ArrivalTime,
                FlightNumberPrefix = detailDto.FlightNumberPrefix,
                FlightNumberSuffix = detailDto.FlightNumberSuffix,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FlightTemplateDetails.CreateAsync(detail);
        }

        _logger.LogInformation("Created flight template: {TemplateName} with {Count} details",
            dto.Name, dto.Details.Count);

        // Reload with details
        var result = await _unitOfWork.FlightScheduleTemplates.GetByIdWithDetailsAsync(createdTemplate.Id);
        return MapToDto(result!);
    }

    public async Task<FlightScheduleTemplateDto> UpdateTemplateAsync(int templateId, CreateFlightTemplateDto dto)
    {
        var template = await _unitOfWork.FlightScheduleTemplates.GetByIdAsync(templateId);
        if (template == null)
        {
            throw new NotFoundException($"Template {templateId} not found");
        }

        // Update template
        template.Name = dto.Name;
        template.Description = dto.Description;
        template.IsActive = dto.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.FlightScheduleTemplates.UpdateAsync(template);

        // Delete old details
        await _unitOfWork.FlightTemplateDetails.DeleteByTemplateIdAsync(templateId);

        // Create new details
        foreach (var detailDto in dto.Details)
        {
            await ValidateFlightDetailAsync(detailDto);

            var detail = new FlightTemplateDetail
            {
                TemplateId = templateId,
                RouteId = detailDto.RouteId,
                AircraftId = detailDto.AircraftId,
                DayOfWeek = detailDto.DayOfWeek,
                DepartureTime = detailDto.DepartureTime,
                ArrivalTime = detailDto.ArrivalTime,
                FlightNumberPrefix = detailDto.FlightNumberPrefix,
                FlightNumberSuffix = detailDto.FlightNumberSuffix,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FlightTemplateDetails.CreateAsync(detail);
        }

        _logger.LogInformation("Updated flight template: {TemplateId}", templateId);

        // Reload with details
        var result = await _unitOfWork.FlightScheduleTemplates.GetByIdWithDetailsAsync(templateId);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        var template = await _unitOfWork.FlightScheduleTemplates.GetByIdAsync(templateId);
        if (template == null)
        {
            return false;
        }

        // Delete details first
        await _unitOfWork.FlightTemplateDetails.DeleteByTemplateIdAsync(templateId);

        // Then delete template
        await _unitOfWork.FlightScheduleTemplates.DeleteAsync(templateId);

        _logger.LogInformation("Deleted flight template: {TemplateId}", templateId);
        return true;
    }

    // ========== Generate Flights ==========

    public async Task<GenerateFlightsResultDto> GenerateFlightsFromTemplateAsync(GenerateFlightsFromTemplateDto dto)
    {
        var result = new GenerateFlightsResultDto();

        try
        {
            // Validate TemplateId first
            if (dto.TemplateId <= 0)
            {
                throw new ValidationException($"Invalid TemplateId: {dto.TemplateId}. TemplateId must be greater than 0. Please check your request body.");
            }

            // Validate
            if (dto.NumberOfWeeks < 1 || dto.NumberOfWeeks > 52)
            {
                throw new ValidationException("Number of weeks must be between 1 and 52");
            }

            // Get template with details
            var template = await _unitOfWork.FlightScheduleTemplates.GetByIdWithDetailsAsync(dto.TemplateId);
            if (template == null)
            {
                throw new NotFoundException($"Template {dto.TemplateId} not found");
            }

            if (!template.IsActive)
            {
                throw new ValidationException($"Template {dto.TemplateId} is not active");
            }

            if (template.Details == null || !template.Details.Any())
            {
                throw new ValidationException($"Template {dto.TemplateId} has no flight details");
            }

            _logger.LogInformation("Generating flights from template {TemplateId} ({TemplateName}) starting {StartDate} for {Weeks} week(s)",
                dto.TemplateId, template.Name, dto.WeekStartDate, dto.NumberOfWeeks);

            // Generate flights for each week
            for (int week = 0; week < dto.NumberOfWeeks; week++)
            {
                var weekStart = dto.WeekStartDate.AddDays(week * 7);
                await GenerateFlightsForWeekAsync(template, weekStart, result);
            }

            _logger.LogInformation("Flight generation completed: {Generated} generated, {Skipped} skipped",
                result.TotalFlightsGenerated, result.TotalFlightsSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flights from template");
            throw;
        }
    }

    // ========== Private Helper Methods ==========

    private async Task GenerateFlightsForWeekAsync(
        FlightScheduleTemplate template,
        DateTime weekStart,
        GenerateFlightsResultDto result)
    {
        foreach (var detail in template.Details)
        {
            try
            {
                // Calculate actual flight date
                var flightDate = weekStart.AddDays(detail.DayOfWeek);

                // Build departure and arrival DateTime
                var departureDateTime = flightDate.Date + detail.DepartureTime.ToTimeSpan();
                var arrivalDateTime = flightDate.Date + detail.ArrivalTime.ToTimeSpan();

                // ❗ HANDLE OVERNIGHT FLIGHTS
                if (detail.ArrivalTime < detail.DepartureTime)
                {
                    arrivalDateTime = arrivalDateTime.AddDays(1);
                    _logger.LogDebug("Overnight flight detected: {FlightNumber} departs {Departure}, arrives {Arrival}",
                        $"{detail.FlightNumberPrefix}{detail.FlightNumberSuffix}",
                        departureDateTime,
                        arrivalDateTime);
                }

                // Build flight number
                var flightNumber = $"{detail.FlightNumberPrefix}{detail.FlightNumberSuffix}";

                // ❗ CHECK 1: Flight number duplicate on same day
                if (await IsFlightNumberDuplicateAsync(flightNumber, flightDate))
                {
                    result.TotalFlightsSkipped++;
                    result.Warnings.Add($"Skipped {flightNumber} on {flightDate:yyyy-MM-dd}: Flight number already exists");
                    continue;
                }

                // ❗ CHECK 2: Aircraft conflict
                if (await HasAircraftConflictAsync(detail.AircraftId, departureDateTime, arrivalDateTime))
                {
                    result.TotalFlightsSkipped++;
                    result.Warnings.Add($"Skipped {flightNumber} on {flightDate:yyyy-MM-dd}: Aircraft {detail.AircraftId} has conflicting flight");
                    continue;
                }

                // ❗ STEP 1: Get or create FlightDefinition
                var arrivalOffsetDays = detail.ArrivalTime < detail.DepartureTime ? 1 : 0;
                var flightDefinition = await _unitOfWork.FlightDefinitions.FindOrCreateAsync(
                    flightNumber,
                    detail.RouteId,
                    detail.AircraftId,
                    detail.DepartureTime,
                    detail.ArrivalTime,
                    arrivalOffsetDays,
                    operatingDays: 127 // Default: every day
                );

                // ❗ STEP 2: Create Flight instance
                var flight = new Flight
                {
                    FlightDefinitionId = flightDefinition.Id,
                    DepartureTime = departureDateTime,
                    ArrivalTime = arrivalDateTime,
                    ActualAircraftId = null, // Use default from FlightDefinition
                    Status = 0, // Scheduled
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Flights.CreateAsync(flight);

                // ❗ STEP 3: Create seat inventory for this flight
                await CreateSeatInventoryForFlightAsync(flight, detail.AircraftId);

                result.TotalFlightsGenerated++;

                _logger.LogDebug("Generated flight: {FlightNumber} on {Date} from {Departure} to {Arrival}",
                    flightNumber, flightDate, departureDateTime, arrivalDateTime);            }
            catch (Exception ex)
            {
                result.TotalFlightsSkipped++;
                result.Errors.Add($"Error generating flight from detail {detail.Id}: {ex.Message}");
                _logger.LogError(ex, "Error generating flight from detail {DetailId}", detail.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<bool> IsFlightNumberDuplicateAsync(string flightNumber, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        // Get FlightDefinition by flight number
        var flightDefinition = await _unitOfWork.FlightDefinitions.GetByFlightNumberAsync(flightNumber);
        if (flightDefinition == null)
        {
            return false; // No definition exists, so no duplicate
        }

        // Check if any Flight with this FlightDefinitionId exists on this date
        var flights = await _unitOfWork.Flights.GetAllAsync();
        return flights.Any(f =>
            f.FlightDefinitionId == flightDefinition.Id &&
            f.DepartureTime >= startOfDay &&
            f.DepartureTime < endOfDay);
    }

    private async Task<bool> HasAircraftConflictAsync(int aircraftId, DateTime newDeparture, DateTime newArrival)
    {
        var flights = await _unitOfWork.Flights.GetAllAsync();

        return flights.Any(f =>
            (f.ActualAircraftId == aircraftId || f.FlightDefinition.DefaultAircraftId == aircraftId) &&
            // Check time overlap: newDeparture < existingArrival AND newArrival > existingDeparture
            newDeparture < f.ArrivalTime &&
            newArrival > f.DepartureTime);
    }

    private async Task CreateSeatInventoryForFlightAsync(Flight flight, int aircraftId)
    {
        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(aircraftId);
        if (aircraft == null)
        {
            _logger.LogWarning("Aircraft {AircraftId} not found, skipping seat inventory creation", aircraftId);
            return;
        }

        // Get aircraft seat templates
        var allAircraft = await _unitOfWork.Aircraft.GetAllAsync();
        var aircraftWithTemplates = allAircraft.FirstOrDefault(a => a.Id == aircraftId);

        if (aircraftWithTemplates?.SeatTemplates == null || !aircraftWithTemplates.SeatTemplates.Any())
        {
            _logger.LogWarning("No seat templates found for aircraft {AircraftId}", aircraftId);
            return;
        }

        foreach (var template in aircraftWithTemplates.SeatTemplates.Where(t => !t.IsDeleted))
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

    private async Task ValidateFlightDetailAsync(CreateFlightTemplateDetailDto detail)
    {
        if (detail.DayOfWeek < 0 || detail.DayOfWeek > 6)
        {
            throw new ValidationException("DayOfWeek must be between 0 and 6");
        }

        var route = await _unitOfWork.Routes.GetByIdAsync(detail.RouteId);
        if (route == null)
        {
            throw new NotFoundException($"Route {detail.RouteId} not found");
        }

        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(detail.AircraftId);
        if (aircraft == null)
        {
            throw new NotFoundException($"Aircraft {detail.AircraftId} not found");
        }

        // Allow either prefix+suffix OR just prefix (as full flight number)
        if (string.IsNullOrWhiteSpace(detail.FlightNumberPrefix))
        {
            throw new ValidationException("Flight number prefix is required");
        }

        // If suffix is empty, use prefix as full flight number
        if (string.IsNullOrWhiteSpace(detail.FlightNumberSuffix))
        {
            _logger.LogDebug("FlightNumberSuffix is empty, using FlightNumberPrefix as full flight number");
            detail.FlightNumberSuffix = ""; // Set to empty string instead of null
        }
    }

    private FlightScheduleTemplateDto MapToDto(FlightScheduleTemplate template)
    {
        return new FlightScheduleTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Details = template.Details?.Select(d => new FlightTemplateDetailDto
            {
                Id = d.Id,
                TemplateId = d.TemplateId,
                DayOfWeek = d.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(d.DayOfWeek),
                DepartureTime = d.DepartureTime,
                ArrivalTime = d.ArrivalTime,
                FlightNumber = $"{d.FlightNumberPrefix}{d.FlightNumberSuffix}",
                RouteId = d.RouteId,
                AircraftId = d.AircraftId,
                RouteName = d.Route != null ? $"{d.Route.DepartureAirport?.Code} → {d.Route.ArrivalAirport?.Code}" : null,
                AircraftName = d.Aircraft?.Model
            }).ToList() ?? new List<FlightTemplateDetailDto>()
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
