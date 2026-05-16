namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class FlightAdminService : IFlightAdminService
{
    private readonly ILogger<FlightAdminService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IEmailService _emailService;

    public FlightAdminService(
        ILogger<FlightAdminService> logger,
        IUnitOfWork unitOfWork,
        IBackgroundJobService backgroundJobService,
        IEmailService emailService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _backgroundJobService = backgroundJobService;
        _emailService = emailService;
    }

    public async Task<FlightManagementResponse> CreateFlightAsync(CreateFlightDto dto)
    {
        if (dto.DepartureTime >= dto.ArrivalTime)
        {
            throw new ValidationException("Arrival time must be later than departure time.");
        }

        var route = await _unitOfWork.Routes.GetByIdAsync(dto.RouteId);
        if (route == null || route.IsDeleted)
        {
            throw new NotFoundException($"Route {dto.RouteId} not found.");
        }

        var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(dto.AircraftId);
        if (aircraft == null)
        {
            throw new NotFoundException($"Aircraft {dto.AircraftId} not found.");
        }

        var definition = await _unitOfWork.FlightDefinitions.FindOrCreateAsync(
            dto.FlightNumber,
            dto.RouteId,
            dto.AircraftId,
            TimeOnly.FromDateTime(dto.DepartureTime),
            TimeOnly.FromDateTime(dto.ArrivalTime),
            dto.ArrivalTime.Date > dto.DepartureTime.Date ? 1 : 0);

        var existing = await _unitOfWork.Flights.ExistsAsync(dto.FlightNumber, dto.DepartureTime, dto.RouteId, dto.AircraftId);
        if (existing)
        {
            throw new ValidationException("Flight already exists with the same number, route, aircraft and departure time.");
        }

        var flight = new Flight
        {
            FlightDefinitionId = definition.Id,
            FlightNumber = dto.FlightNumber,
            RouteId = dto.RouteId,
            AircraftId = dto.AircraftId,
            ArrivalOffsetDays = dto.ArrivalTime.Date > dto.DepartureTime.Date ? 1 : 0,
            DepartureTime = dto.DepartureTime,
            ArrivalTime = dto.ArrivalTime,
            Status = dto.IsActive ? 0 : 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _unitOfWork.Flights.CreateAsync(flight);
        await CreateSeatInventoryForFlightAsync(created, dto.AircraftId);
        var createdWithDetails = await _unitOfWork.Flights.GetByIdWithDetailsAsync(created.Id) ?? created;
        return await MapFlightResponseAsync(createdWithDetails);
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

                var response = await CreateFlightAsync(new CreateFlightDto
                {
                    FlightNumber = flightNumber,
                    RouteId = pattern.RouteId,
                    AircraftId = pattern.AircraftId,
                    DepartureTime = departure,
                    ArrivalTime = arrival,
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

        if (dto.AircraftId.HasValue)
        {
            var aircraft = await _unitOfWork.Aircraft.GetByIdAsync(dto.AircraftId.Value);
            if (aircraft == null)
            {
                throw new NotFoundException($"Aircraft {dto.AircraftId.Value} not found.");
            }

            flight.AircraftId = dto.AircraftId.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.FlightNumber))
        {
            flight.FlightNumber = dto.FlightNumber;
        }

        flight.DepartureTime = dto.DepartureTime ?? flight.DepartureTime;
        flight.ArrivalTime = dto.ArrivalTime ?? flight.ArrivalTime;
        if (flight.ArrivalTime <= flight.DepartureTime)
        {
            throw new ValidationException("Arrival time must be later than departure time.");
        }

        if (dto.IsActive.HasValue)
        {
            flight.Status = dto.IsActive.Value ? 0 : 1;
        }

        flight.ArrivalOffsetDays = flight.ArrivalTime.Date > flight.DepartureTime.Date ? 1 : 0;
        flight.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Flights.UpdateAsync(flight);
        return true;
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

        var cancelledBookings = 0;
        var refundQueuedBookings = 0;
        var notificationSentBookings = 0;
        var emailJobs = new List<Task>();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var booking in allBookings)
            {
                var passengers = await _unitOfWork.BookingPassengers.GetByBookingIdAsync(booking.Id);
                var groupedSeatCounts = passengers
                    .GroupBy(p => p.FlightSeatInventoryId)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var groupedSeat in groupedSeatCounts)
                {
                    var inventory = await _unitOfWork.FlightSeatInventories.GetByIdAsync(groupedSeat.Key);
                    if (inventory == null)
                    {
                        continue;
                    }

                    if (booking.Status == (int)BookingStatus.Pending)
                    {
                        inventory.ReleaseHeldSeats(groupedSeat.Value);
                    }
                    else
                    {
                        inventory.CancelSoldSeats(groupedSeat.Value);
                    }

                    await _unitOfWork.FlightSeatInventories.UpdateAsync(inventory);
                }

                var hasCompletedPayment = (await _unitOfWork.Payments.GetByBookingIdAsync(booking.Id))
                    .Any(p => p.Status == 1 && !p.IsDeleted);

                booking.Status = (int)BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.UpdateAsync(booking);
                cancelledBookings++;

                if (hasCompletedPayment)
                {
                    _backgroundJobService.EnqueueVnpayRefund(
                        booking.Id,
                        $"Admin cancelled flight {flight.FlightNumber}. Reason: {reason}");
                    refundQueuedBookings++;
                }

                if (!string.IsNullOrWhiteSpace(booking.ContactEmail))
                {
                    emailJobs.Add(_emailService.SendNotificationAsync(
                        booking.ContactEmail,
                        $"Flight {flight.FlightNumber} cancellation notice",
                        $"<p>Your booking <strong>{booking.BookingCode}</strong> has been cancelled because flight <strong>{flight.FlightNumber}</strong> was cancelled by admin.</p><p>Reason: {reason}</p><p>If your payment was completed, refund processing has been queued.</p>"));
                    notificationSentBookings++;
                }
            }

            flight.Cancel();
            await _unitOfWork.Flights.UpdateAsync(flight);
        });

        if (emailJobs.Count > 0)
        {
            await Task.WhenAll(emailJobs);
        }

        _logger.LogInformation(
            "Admin cancelled flight {FlightId} ({FlightNumber}). CancelledBookings={CancelledBookings}, RefundQueued={RefundQueuedBookings}, Notifications={NotificationSentBookings}",
            flight.Id,
            flight.FlightNumber,
            cancelledBookings,
            refundQueuedBookings,
            notificationSentBookings);

        return new CancelFlightAdminResponse
        {
            FlightId = flight.Id,
            FlightNumber = flight.FlightNumber,
            CancelledBookings = cancelledBookings,
            RefundQueuedBookings = refundQueuedBookings,
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
}
