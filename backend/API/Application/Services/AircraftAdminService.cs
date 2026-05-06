namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class AircraftAdminService : IAircraftAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AircraftAdminService> _logger;

    public AircraftAdminService(
        IUnitOfWork unitOfWork,
        ILogger<AircraftAdminService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<AircraftManagementResponse>> GetAllAircraftAsync(int page, int pageSize, bool includeDeleted = false)
    {
        _logger.LogInformation("Getting all aircraft (page {Page}, pageSize {PageSize}, includeDeleted {IncludeDeleted})", 
            page, pageSize, includeDeleted);

        var allAircraft = await _unitOfWork.Aircraft.GetAllAsync();
        
        var aircraft = allAircraft
            .Where(a => includeDeleted || !a.IsDeleted)
            .OrderBy(a => a.Model)
            .ThenBy(a => a.RegistrationNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var response = new List<AircraftManagementResponse>();

        foreach (var ac in aircraft)
        {
            // Count active flights for this aircraft
            var flights = await _unitOfWork.Flights.GetAllAsync();
            var activeFlightsCount = flights.Count(f => 
                (f.ActualAircraftId == ac.Id || f.FlightDefinition.DefaultAircraftId == ac.Id) &&
                f.DepartureTime > DateTime.UtcNow);

            response.Add(MapToResponse(ac, activeFlightsCount));
        }

        return response;
    }

    public async Task<AircraftManagementResponse?> GetAircraftByIdAsync(int aircraftId)
    {
        _logger.LogInformation("Getting aircraft {AircraftId}", aircraftId);

        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(aircraftId);
        if (aircraft == null)
        {
            return null;
        }

        // Count active flights
        var flights = await _unitOfWork.Flights.GetAllAsync();
        var activeFlightsCount = flights.Count(f => 
            (f.ActualAircraftId == aircraftId || f.FlightDefinition.DefaultAircraftId == aircraftId) &&
            f.DepartureTime > DateTime.UtcNow);

        return MapToResponse(aircraft, activeFlightsCount);
    }

    public async Task<AircraftManagementResponse> CreateAircraftAsync(CreateAircraftDto dto)
    {
        _logger.LogInformation("Creating aircraft: {Model} ({RegistrationNumber})", dto.Model, dto.RegistrationNumber);

        // Validate registration number is unique
        var allAircraft = await _unitOfWork.Aircraft.GetAllAsync();
        if (allAircraft.Any(a => a.RegistrationNumber == dto.RegistrationNumber && !a.IsDeleted))
        {
            throw new ValidationException($"Aircraft with registration number {dto.RegistrationNumber} already exists");
        }

        // Validate seat templates
        if (dto.SeatTemplates.Any())
        {
            var totalSeatsFromTemplates = dto.SeatTemplates.Sum(st => st.DefaultSeatCount);
            if (totalSeatsFromTemplates != dto.TotalSeats)
            {
                throw new ValidationException($"Sum of seat counts ({totalSeatsFromTemplates}) must equal total seats ({dto.TotalSeats})");
            }

            // Validate seat classes exist
            foreach (var template in dto.SeatTemplates)
            {
                var seatClass = await _unitOfWork.SeatClasses.GetByIdAsync(template.SeatClassId);
                if (seatClass == null)
                {
                    throw new NotFoundException($"Seat class {template.SeatClassId} not found");
                }
            }
        }

        // Create aircraft
        var aircraft = new Aircraft
        {
            RegistrationNumber = dto.RegistrationNumber,
            Model = dto.Model,
            TotalSeats = dto.TotalSeats,
            IsActive = dto.IsActive,
            IsDeleted = false
        };

        var createdAircraft = await _unitOfWork.Aircraft.CreateAsync(aircraft);

        // Create seat templates
        foreach (var templateDto in dto.SeatTemplates)
        {
            var seatTemplate = new AircraftSeatTemplate
            {
                AircraftId = createdAircraft.Id,
                SeatClassId = templateDto.SeatClassId,
                DefaultSeatCount = templateDto.DefaultSeatCount,
                DefaultBasePrice = templateDto.DefaultBasePrice,
                IsDeleted = false
            };

            // Note: Need to add AircraftSeatTemplate repository to UnitOfWork
            // For now, we'll use EF context directly through Aircraft navigation
            createdAircraft.SeatTemplates ??= new List<AircraftSeatTemplate>();
            createdAircraft.SeatTemplates.Add(seatTemplate);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Aircraft created successfully: {AircraftId}", createdAircraft.Id);

        return MapToResponse(createdAircraft, 0);
    }

    public async Task<AircraftManagementResponse> UpdateAircraftAsync(int aircraftId, UpdateAircraftDto dto)
    {
        _logger.LogInformation("Updating aircraft {AircraftId}", aircraftId);

        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(aircraftId);
        if (aircraft == null)
        {
            throw new NotFoundException($"Aircraft {aircraftId} not found");
        }

        if (aircraft.IsDeleted)
        {
            throw new ValidationException("Cannot update deleted aircraft. Restore it first.");
        }

        // Update basic info
        if (!string.IsNullOrWhiteSpace(dto.RegistrationNumber))
        {
            // Check uniqueness
            var allAircraft = await _unitOfWork.Aircraft.GetAllAsync();
            if (allAircraft.Any(a => a.RegistrationNumber == dto.RegistrationNumber && a.Id != aircraftId && !a.IsDeleted))
            {
                throw new ValidationException($"Aircraft with registration number {dto.RegistrationNumber} already exists");
            }
            aircraft.RegistrationNumber = dto.RegistrationNumber;
        }

        if (!string.IsNullOrWhiteSpace(dto.Model))
        {
            aircraft.Model = dto.Model;
        }

        if (dto.TotalSeats.HasValue)
        {
            aircraft.TotalSeats = dto.TotalSeats.Value;
        }

        if (dto.IsActive.HasValue)
        {
            aircraft.IsActive = dto.IsActive.Value;
        }

        // Update seat templates if provided
        if (dto.SeatTemplates != null)
        {
            // Validate total seats
            var totalSeatsFromTemplates = dto.SeatTemplates.Sum(st => st.DefaultSeatCount);
            if (totalSeatsFromTemplates != aircraft.TotalSeats)
            {
                throw new ValidationException($"Sum of seat counts ({totalSeatsFromTemplates}) must equal total seats ({aircraft.TotalSeats})");
            }

            // Soft delete old templates
            if (aircraft.SeatTemplates != null)
            {
                foreach (var oldTemplate in aircraft.SeatTemplates.Where(st => !st.IsDeleted))
                {
                    oldTemplate.SoftDelete();
                }
            }

            // Create new templates
            aircraft.SeatTemplates ??= new List<AircraftSeatTemplate>();
            foreach (var templateDto in dto.SeatTemplates)
            {
                var seatClass = await _unitOfWork.SeatClasses.GetByIdAsync(templateDto.SeatClassId);
                if (seatClass == null)
                {
                    throw new NotFoundException($"Seat class {templateDto.SeatClassId} not found");
                }

                aircraft.SeatTemplates.Add(new AircraftSeatTemplate
                {
                    AircraftId = aircraftId,
                    SeatClassId = templateDto.SeatClassId,
                    DefaultSeatCount = templateDto.DefaultSeatCount,
                    DefaultBasePrice = templateDto.DefaultBasePrice,
                    IsDeleted = false
                });
            }
        }

        await _unitOfWork.Aircraft.UpdateAsync(aircraft);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Aircraft {AircraftId} updated successfully", aircraftId);

        // Count active flights
        var flights = await _unitOfWork.Flights.GetAllAsync();
        var activeFlightsCount = flights.Count(f => 
            (f.ActualAircraftId == aircraftId || f.FlightDefinition.DefaultAircraftId == aircraftId) &&
            f.DepartureTime > DateTime.UtcNow);

        return MapToResponse(aircraft, activeFlightsCount);
    }

    public async Task<bool> DeleteAircraftAsync(int aircraftId)
    {
        _logger.LogInformation("Deleting aircraft {AircraftId}", aircraftId);

        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(aircraftId);
        if (aircraft == null)
        {
            return false;
        }

        // Check if aircraft has active flights
        var flights = await _unitOfWork.Flights.GetAllAsync();
        var hasActiveFlights = flights.Any(f => 
            (f.ActualAircraftId == aircraftId || f.FlightDefinition.DefaultAircraftId == aircraftId) &&
            f.DepartureTime > DateTime.UtcNow);

        if (hasActiveFlights)
        {
            throw new ValidationException("Cannot delete aircraft with active flights");
        }

        // Soft delete
        aircraft.SoftDelete();
        await _unitOfWork.Aircraft.UpdateAsync(aircraft);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Aircraft {AircraftId} deleted successfully", aircraftId);
        return true;
    }

    public async Task<bool> RestoreAircraftAsync(int aircraftId)
    {
        _logger.LogInformation("Restoring aircraft {AircraftId}", aircraftId);

        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(aircraftId);
        if (aircraft == null)
        {
            return false;
        }

        if (!aircraft.IsDeleted)
        {
            throw new ValidationException("Aircraft is not deleted");
        }

        aircraft.Restore();
        await _unitOfWork.Aircraft.UpdateAsync(aircraft);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Aircraft {AircraftId} restored successfully", aircraftId);
        return true;
    }

    public async Task<AircraftStatisticsResponse> GetAircraftStatisticsAsync(int aircraftId)
    {
        _logger.LogInformation("Getting statistics for aircraft {AircraftId}", aircraftId);

        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(aircraftId);
        if (aircraft == null)
        {
            throw new NotFoundException($"Aircraft {aircraftId} not found");
        }

        var flights = await _unitOfWork.Flights.GetAllAsync();
        var aircraftFlights = flights.Where(f => 
            f.ActualAircraftId == aircraftId || f.FlightDefinition.DefaultAircraftId == aircraftId).ToList();

        var totalFlights = aircraftFlights.Count;
        var activeFlights = aircraftFlights.Count(f => f.DepartureTime > DateTime.UtcNow);
        var completedFlights = aircraftFlights.Count(f => f.DepartureTime <= DateTime.UtcNow);

        // Calculate bookings and revenue
        var bookings = await _unitOfWork.Bookings.GetAllAsync();
        var aircraftBookings = bookings.Where(b => 
            aircraftFlights.Any(f => f.Id == b.OutboundFlightId || f.Id == b.ReturnFlightId)).ToList();

        var totalBookings = aircraftBookings.Count;
        var totalRevenue = aircraftBookings.Where(b => b.Status == 1).Sum(b => b.FinalAmount); // Status 1 = Confirmed

        // Calculate occupancy rate
        double averageOccupancyRate = 0;
        if (completedFlights > 0)
        {
            var completedFlightIds = aircraftFlights.Where(f => f.DepartureTime <= DateTime.UtcNow).Select(f => f.Id).ToList();
            var seatInventories = new List<FlightSeatInventory>();
            
            foreach (var flightId in completedFlightIds)
            {
                var inventories = await _unitOfWork.FlightSeatInventories.GetByFlightIdAsync(flightId);
                seatInventories.AddRange(inventories);
            }

            if (seatInventories.Any())
            {
                var totalSeats = seatInventories.Sum(s => s.TotalSeats);
                var soldSeats = seatInventories.Sum(s => s.SoldSeats);
                averageOccupancyRate = totalSeats > 0 ? (double)soldSeats / totalSeats * 100 : 0;
            }
        }

        return new AircraftStatisticsResponse
        {
            AircraftId = aircraftId,
            RegistrationNumber = aircraft.RegistrationNumber,
            Model = aircraft.Model,
            TotalFlights = totalFlights,
            ActiveFlights = activeFlights,
            CompletedFlights = completedFlights,
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            AverageOccupancyRate = Math.Round(averageOccupancyRate, 2)
        };
    }

    // Helper method
    private AircraftManagementResponse MapToResponse(Aircraft aircraft, int activeFlightsCount)
    {
        return new AircraftManagementResponse
        {
            AircraftId = aircraft.Id,
            RegistrationNumber = aircraft.RegistrationNumber,
            Model = aircraft.Model,
            TotalSeats = aircraft.TotalSeats,
            IsActive = aircraft.IsActive,
            IsDeleted = aircraft.IsDeleted,
            DeletedAt = aircraft.DeletedAt,
            ActiveFlightsCount = activeFlightsCount,
            SeatTemplates = aircraft.SeatTemplates?
                .Where(st => !st.IsDeleted)
                .Select(st => new AircraftSeatTemplateResponse
                {
                    Id = st.Id,
                    SeatClassId = st.SeatClassId,
                    SeatClassName = st.SeatClass?.Name ?? "Unknown",
                    DefaultSeatCount = st.DefaultSeatCount,
                    DefaultBasePrice = st.DefaultBasePrice,
                    IsDeleted = st.IsDeleted
                }).ToList() ?? new List<AircraftSeatTemplateResponse>()
        };
    }
}
