namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class FlightAdminService : IFlightAdminService
{
    private readonly ILogger<FlightAdminService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly INotificationService? _notificationService;
    private readonly FlightBookingDbContext _dbContext;

    public FlightAdminService(
        ILogger<FlightAdminService> logger,
        IUnitOfWork unitOfWork,
        IBackgroundJobService backgroundJobService,
        IAuditLogService auditLogService,
        IEmailService emailService,
        FlightBookingDbContext dbContext,
        INotificationService? notificationService = null)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _backgroundJobService = backgroundJobService;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<FlightManagementResponse> CreateFlightAsync(CreateFlightDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(dto.FlightDefinitionId);
            if (definition == null || !definition.IsActive)
            {
                throw new NotFoundException($"Flight definition {dto.FlightDefinitionId} not found.");
            }

            var route = await _unitOfWork.Routes.GetByIdAsync(definition.RouteId);
            if (route == null || route.IsDeleted)
            {
                throw new NotFoundException($"Route {definition.RouteId} not found.");
            }

            var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(definition.DefaultAircraftId);
            if (aircraft == null || aircraft.IsDeleted)
            {
                throw new NotFoundException($"Aircraft {definition.DefaultAircraftId} not found.");
            }

            var departureTime = dto.DepartureDate.ToDateTime(dto.DepartureTime, DateTimeKind.Utc);
            var arrivalTime = CalculateArrivalTime(departureTime, definition);

            var existing = await _unitOfWork.Flights.ExistsAsync(
                definition.FlightNumber,
                departureTime,
                definition.RouteId,
                definition.DefaultAircraftId);
            if (existing)
            {
                throw new ValidationException("Flight already exists for this definition and departure time.");
            }

            var flight = new Flight
            {
                FlightDefinitionId = definition.Id,
                FlightNumber = definition.FlightNumber,
                RouteId = definition.RouteId,
                AircraftId = definition.DefaultAircraftId,
                ArrivalOffsetDays = definition.ArrivalOffsetDays,
                DepartureTime = departureTime,
                ArrivalTime = arrivalTime,
                Status = dto.IsActive ? 0 : 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _unitOfWork.Flights.CreateAsync(flight);
            await CreateSeatInventoryForFlightAsync(created, definition.DefaultAircraftId);
            var createdWithDetails = await _unitOfWork.Flights.GetByIdWithDetailsAsync(created.Id) ?? created;
            return await MapFlightResponseAsync(createdWithDetails);
        });
    }

    public async Task<List<FlightManagementResponse>> CreateWeeklyScheduleAsync(CreateWeeklyScheduleDto dto)
    {
        if (dto.Flights.Count == 0)
        {
            throw new ValidationException("Flights list cannot be empty.");
        }

        var weeks = Math.Max(0, dto.AutoGenerateWeeks);
        var responses = new List<FlightManagementResponse>();

        for (var weekOffset = 0; weekOffset <= weeks; weekOffset++)
        {
            var weekStart = dto.WeekStartDate.AddDays(weekOffset * 7);

            foreach (var pattern in dto.Flights)
            {
                if (pattern.DayOfWeek < 0 || pattern.DayOfWeek > 6)
                {
                    throw new ValidationException("DayOfWeek must be between 0 and 6.");
                }

                var flightDate = weekStart.AddDays(pattern.DayOfWeek);
                var departure = flightDate.ToDateTime(pattern.DepartureTimeOfDay, DateTimeKind.Utc);
                var arrival = flightDate.ToDateTime(pattern.ArrivalTimeOfDay, DateTimeKind.Utc);
                if (arrival <= departure)
                {
                    arrival = arrival.AddDays(1);
                }

                var flightNumber = !string.IsNullOrWhiteSpace(pattern.FlightNumber)
                    ? pattern.FlightNumber
                    : $"{pattern.FlightNumberPrefix}{flightDate:MMdd}{pattern.DayOfWeek}";

                var exists = await _unitOfWork.Flights.ExistsAsync(flightNumber, departure, pattern.RouteId, pattern.AircraftId);
                if (exists)
                {
                    continue;
                }

                var definition = await _unitOfWork.FlightDefinitions.FindOrCreateAsync(
                    flightNumber,
                    pattern.RouteId,
                    pattern.AircraftId,
                    pattern.DepartureTimeOfDay,
                    pattern.ArrivalTimeOfDay,
                    arrival.Date > departure.Date ? 1 : 0);

                var response = await CreateFlightAsync(new CreateFlightDto
                {
                    FlightDefinitionId = definition.Id,
                    DepartureDate = DateOnly.FromDateTime(departure),
                    DepartureTime = TimeOnly.FromDateTime(departure),
                    IsActive = pattern.IsActive
                });

                responses.Add(response);
            }
        }

        return responses;
    }

    public async Task<bool> UpdateFlightAsync(int flightId, UpdateFlightDto dto)
    {
        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
        if (flight == null || flight.IsDeleted)
        {
            throw new NotFoundException($"Flight {flightId} not found.");
        }

        if (dto.ActualAircraftId.HasValue)
        {
            var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(dto.ActualAircraftId.Value);
            if (aircraft == null || aircraft.IsDeleted)
            {
                throw new NotFoundException($"Aircraft {dto.ActualAircraftId.Value} not found.");
            }

            flight.ActualAircraftId = dto.ActualAircraftId.Value;
        }

        var definition = await _unitOfWork.FlightDefinitions.GetByIdAsync(flight.FlightDefinitionId);
        if (definition == null)
        {
            throw new ValidationException("Flight definition not found for this flight");
        }

        var currentDepartureDate = DateOnly.FromDateTime(flight.DepartureTime);
        var currentDepartureClock = TimeOnly.FromDateTime(flight.DepartureTime);
        var nextDepartureDate = dto.DepartureDate ?? currentDepartureDate;
        var nextDepartureClock = dto.DepartureTime ?? currentDepartureClock;
        var nextDeparture = nextDepartureDate.ToDateTime(nextDepartureClock, DateTimeKind.Utc);
        var nextArrival = CalculateArrivalTime(nextDeparture, definition);
        flight.DepartureTime = nextDeparture;
        flight.ArrivalTime = nextArrival;
        flight.ArrivalOffsetDays = definition.ArrivalOffsetDays;

        if (dto.IsActive.HasValue)
        {
            flight.Status = dto.IsActive.Value ? 0 : 1;
        }

        flight.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Flights.UpdateAsync(flight);
        return true;
    }

    public async Task<bool> UpdateFlightPricesAsync(int flightId, UpdateFlightPricesDto dto, int? adminUserId)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ValidationException("Price update items cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new ValidationException("Reason is required for price updates.");
        }

        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
        if (flight == null || flight.IsDeleted)
        {
            throw new NotFoundException($"Flight {flightId} not found.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var item in dto.Items)
            {
                if (item.NewPrice <= 0)
                {
                    throw new ValidationException("New price must be greater than zero.");
                }

                var inventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(flightId, item.SeatClassId);
                if (inventory == null)
                {
                    throw new NotFoundException($"Seat inventory not found for seat class {item.SeatClassId}.");
                }

                var oldValues = JsonSerializer.Serialize(new
                {
                    flightId,
                    seatClassId = item.SeatClassId,
                    oldPrice = inventory.CurrentPrice
                });

                inventory.CurrentPrice = item.NewPrice;
                inventory.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.FlightSeatInventories.UpdateAsync(inventory);

                var newValues = JsonSerializer.Serialize(new
                {
                    flightId,
                    seatClassId = item.SeatClassId,
                    newPrice = item.NewPrice,
                    reason = dto.Reason.Trim()
                });

                await _auditLogService.LogActionAsync(
                    adminUserId,
                    "PRICE_UPDATE",
                    "FlightSeatInventory",
                    inventory.Id,
                    oldValues,
                    newValues,
                    null);
            }

            return true;
        });
    }

    public async Task<bool> DeleteFlightAsync(int flightId)
    {
        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
        if (flight == null || flight.IsDeleted)
        {
            throw new NotFoundException($"Flight {flightId} not found.");
        }

        flight.SoftDelete();
        await _unitOfWork.Flights.UpdateAsync(flight);
        return true;
    }

    public async Task<CancelFlightAdminResponse> CancelFlightAsync(int flightId, CancelFlightAdminDto dto)
    {
        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
        if (flight == null || flight.IsDeleted)
        {
            throw new NotFoundException($"Flight {flightId} not found.");
        }

        if (flight.Status == 1)
        {
            return new CancelFlightAdminResponse
            {
                FlightId = flight.Id,
                FlightNumber = flight.FlightNumber,
                CancelledBookings = 0,
                RefundQueuedBookings = 0,
                NotificationSentBookings = 0
            };
        }

        var reason = string.IsNullOrWhiteSpace(dto.Reason)
            ? "Flight cancelled by admin operation"
            : dto.Reason.Trim();

        var allBookings = (await _unitOfWork.Bookings.GetAllAsync())
            .Where(b => !b.IsDeleted
                && (b.OutboundFlightId == flightId || b.ReturnFlightId == flightId)
                && (b.Status == (int)BookingStatus.Pending
                    || b.Status == (int)BookingStatus.Confirmed
                    || b.Status == (int)BookingStatus.CheckedIn))
            .ToList();

        var affectedBookings = 0;
        var pendingDecisionBookings = 0;
        var notificationSentBookings = 0;
        var cancellationNotifications = new List<(int UserId, string Email, int BookingId, string Title, string Content)>();
        var decisionDeadline = DateTime.UtcNow.AddHours(24);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var booking in allBookings)
            {
                var affectedLegTypes = new List<int>();
                if (booking.OutboundFlightId == flightId)
                {
                    affectedLegTypes.Add(0);
                }
                if (booking.ReturnFlightId == flightId)
                {
                    affectedLegTypes.Add(1);
                }

                foreach (var legType in affectedLegTypes)
                {
                    var existing = await _dbContext.FlightDisruptionDecisions
                        .FirstOrDefaultAsync(d =>
                            !d.IsDeleted &&
                            d.BookingId == booking.Id &&
                            d.AffectedFlightId == flightId &&
                            d.LegType == legType);
                    if (existing != null)
                    {
                        continue;
                    }

                    _dbContext.FlightDisruptionDecisions.Add(new FlightDisruptionDecision
                    {
                        BookingId = booking.Id,
                        UserId = booking.UserId,
                        AffectedFlightId = flightId,
                        LegType = legType,
                        Status = 0,
                        DecisionType = 0,
                        DecisionDeadline = decisionDeadline,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Reason = reason
                    });
                }

                booking.Status = (int)BookingStatus.PendingDisruptionDecision;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.UpdateAsync(booking);
                affectedBookings++;
                pendingDecisionBookings++;

                cancellationNotifications.Add((
                    booking.UserId,
                    booking.ContactEmail,
                    booking.Id,
                    $"Flight {flight.FlightNumber} cancellation notice",
                    $"Your booking {booking.BookingCode} is affected because flight {flight.FlightNumber} was cancelled. Please choose rebook or cancel before {decisionDeadline:yyyy-MM-dd HH:mm} UTC. Rebooking and cancellation are free of charge."));
            }

            await _dbContext.SaveChangesAsync();
            flight.Cancel();
            await _unitOfWork.Flights.UpdateAsync(flight);
        });

        foreach (var notification in cancellationNotifications)
        {
            var sent = false;
            if (_notificationService != null)
            {
                sent = await _notificationService.SendNotificationAsync(
                    notification.UserId,
                    notification.Title,
                    notification.Content,
                    type: "IN_APP",
                    category: "FLIGHT_CANCELLATION",
                    relatedEntityType: "Booking",
                    relatedEntityId: notification.BookingId,
                    sendEmail: true);
            }
            else if (!string.IsNullOrWhiteSpace(notification.Email))
            {
                await _emailService.SendNotificationAsync(notification.Email, notification.Title, notification.Content);
                sent = true;
            }

            if (sent)
            {
                notificationSentBookings++;
            }
        }

        _logger.LogInformation(
            "Admin cancelled flight {FlightId} ({FlightNumber}). CancelledBookings={CancelledBookings}, RefundQueued={RefundQueuedBookings}, Notifications={NotificationSentBookings}",
            flight.Id,
            flight.FlightNumber,
            affectedBookings,
            pendingDecisionBookings,
            notificationSentBookings);

        return new CancelFlightAdminResponse
        {
            FlightId = flight.Id,
            FlightNumber = flight.FlightNumber,
            CancelledBookings = affectedBookings,
            RefundQueuedBookings = pendingDecisionBookings,
            NotificationSentBookings = notificationSentBookings
        };
    }

    public async Task<List<FlightManagementResponse>> GetFlightsAsync(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;

        var flights = (await _unitOfWork.Flights.GetAllAsync())
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.DepartureTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var response = new List<FlightManagementResponse>();
        foreach (var flight in flights)
        {
            response.Add(await MapFlightResponseAsync(flight));
        }

        return response;
    }

    public Task<RouteManagementResponse> CreateRouteAsync(CreateRouteDto dto)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (dto.DepartureAirportId == dto.ArrivalAirportId)
            {
                throw new ValidationException("Departure and arrival airports must be different.");
            }

            var departureAirport = await _unitOfWork.Airports.GetByIdAsync(dto.DepartureAirportId);
            var arrivalAirport = await _unitOfWork.Airports.GetByIdAsync(dto.ArrivalAirportId);
            if (departureAirport == null || arrivalAirport == null)
            {
                throw new NotFoundException("Departure or arrival airport not found.");
            }

            var existingRoutes = await _unitOfWork.Routes.GetByAirportsAsync(dto.DepartureAirportId, dto.ArrivalAirportId);
            if (existingRoutes.Any(r => !r.IsDeleted))
            {
                throw new ValidationException("Route already exists for this airport pair.");
            }

            var route = new Route
            {
                Code = $"{departureAirport.Code}-{arrivalAirport.Code}",
                DepartureAirportId = dto.DepartureAirportId,
                ArrivalAirportId = dto.ArrivalAirportId,
                DistanceKm = dto.DistanceKm,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                IsActive = true
            };

            var created = await _unitOfWork.Routes.CreateAsync(route);

            return new RouteManagementResponse
            {
                RouteId = created.Id,
                DepartureAirport = departureAirport.Code,
                ArrivalAirport = arrivalAirport.Code,
                DistanceKm = created.DistanceKm,
                EstimatedDurationMinutes = created.EstimatedDurationMinutes,
                ActiveFlights = 0,
                IsActive = created.IsActive && !created.IsDeleted
            };
        });
    }

    public async Task<bool> UpdateRouteAsync(int routeId, UpdateRouteDto dto)
    {
        var route = await _unitOfWork.Routes.GetByIdAsync(routeId);
        if (route == null || route.IsDeleted)
        {
            throw new NotFoundException($"Route {routeId} not found.");
        }

        var newDepartureAirportId = dto.DepartureAirportId ?? route.DepartureAirportId;
        var newArrivalAirportId = dto.ArrivalAirportId ?? route.ArrivalAirportId;

        if (newDepartureAirportId == newArrivalAirportId)
        {
            throw new ValidationException("Departure and arrival airports must be different.");
        }

        var departureAirport = await _unitOfWork.Airports.GetByIdAsync(newDepartureAirportId);
        var arrivalAirport = await _unitOfWork.Airports.GetByIdAsync(newArrivalAirportId);
        if (departureAirport == null || arrivalAirport == null)
        {
            throw new NotFoundException("Departure or arrival airport not found.");
        }

        if (newDepartureAirportId != route.DepartureAirportId || newArrivalAirportId != route.ArrivalAirportId)
        {
            var existingRoutes = await _unitOfWork.Routes.GetByAirportsAsync(newDepartureAirportId, newArrivalAirportId);
            if (existingRoutes.Any(r => r.Id != routeId && !r.IsDeleted))
            {
                throw new ValidationException("Route already exists for this airport pair.");
            }
        }

        route.DepartureAirportId = newDepartureAirportId;
        route.ArrivalAirportId = newArrivalAirportId;
        route.Code = $"{departureAirport.Code}-{arrivalAirport.Code}";
        route.DistanceKm = dto.DistanceKm ?? route.DistanceKm;
        route.EstimatedDurationMinutes = dto.EstimatedDurationMinutes ?? route.EstimatedDurationMinutes;

        await _unitOfWork.Routes.UpdateAsync(route);
        return true;
    }

    public async Task<List<RouteManagementResponse>> GetRoutesAsync(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;

        var allRoutes = (await _unitOfWork.Routes.GetAllAsync())
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Code)
            .ToList();

        var activeFlightDefinitionCounts = (await _unitOfWork.FlightDefinitions.GetAllAsync())
            .Where(fd => fd.IsActive)
            .GroupBy(fd => fd.RouteId)
            .ToDictionary(g => g.Key, g => g.Count());

        return allRoutes
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(route => new RouteManagementResponse
            {
                RouteId = route.Id,
                DepartureAirport = route.DepartureAirport?.Code ?? "N/A",
                ArrivalAirport = route.ArrivalAirport?.Code ?? "N/A",
                DistanceKm = route.DistanceKm,
                EstimatedDurationMinutes = route.EstimatedDurationMinutes,
                ActiveFlights = activeFlightDefinitionCounts.TryGetValue(route.Id, out var count) ? count : 0,
                IsActive = route.IsActive && !route.IsDeleted
            })
            .ToList();
    }

    private async Task<FlightManagementResponse> MapFlightResponseAsync(Flight flight)
    {
        var route = flight.Route ?? await _unitOfWork.Routes.GetByIdAsync(flight.RouteId);
        var aircraft = flight.Aircraft ?? await _unitOfWork.Aircraft.GetByIdAsync(flight.AircraftId);

        var seatInventories = await _unitOfWork.FlightSeatInventories.GetByFlightIdAsync(flight.Id);
        var totalSeats = seatInventories.Sum(s => s.TotalSeats);
        var availableSeats = seatInventories.Sum(s => s.AvailableSeats);
        var bookedSeats = totalSeats - availableSeats;

        return new FlightManagementResponse
        {
            FlightId = flight.Id,
            FlightNumber = flight.FlightNumber,
            RouteCode = route != null
                ? $"{route.DepartureAirport?.Code ?? "?"} -> {route.ArrivalAirport?.Code ?? "?"}"
                : "N/A",
            AircraftModel = aircraft?.Model ?? "N/A",
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats,
            BookedSeats = bookedSeats,
            IsActive = flight.Status == 0 && !flight.IsDeleted,
            CreatedAt = flight.CreatedAt
        };
    }

    private async Task CreateSeatInventoryForFlightAsync(Flight flight, int aircraftId)
    {
        var existingInventories = await _unitOfWork.FlightSeatInventories.GetByFlightIdAsync(flight.Id);
        if (existingInventories.Count > 0)
        {
            return;
        }

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

    private static DateTime CalculateArrivalTime(DateTime departureTimeUtc, FlightDefinition definition)
    {
        var departureClock = definition.DepartureTime.ToTimeSpan();
        var arrivalClock = definition.ArrivalTime.ToTimeSpan();
        var arrivalOffset = definition.ArrivalOffsetDays;
        if (arrivalOffset == 0 && arrivalClock <= departureClock)
        {
            arrivalOffset = 1;
        }

        var departureDate = DateOnly.FromDateTime(departureTimeUtc);
        var arrivalDate = departureDate.AddDays(arrivalOffset);
        return arrivalDate.ToDateTime(definition.ArrivalTime, DateTimeKind.Utc);
    }
}
