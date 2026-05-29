namespace API.Application.Services;

using API.Application.Dtos.Ticket;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Common;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly FlightBookingDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<TicketService> _logger;
    private readonly INotificationService? _notificationService;
    private const decimal ChildFareRate = 0.7m;

    public TicketService(
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository,
        IBookingPassengerRepository passengerRepository,
        IFlightRepository flightRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        FlightBookingDbContext dbContext,
        IEmailService emailService,
        ILogger<TicketService> logger,
        INotificationService? notificationService = null)
    {
        _ticketRepository = ticketRepository;
        _bookingRepository = bookingRepository;
        _passengerRepository = passengerRepository;
        _flightRepository = flightRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<List<TicketResponse>> CreateTicketsAsync(int bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
            var passengerById = passengers.ToDictionary(p => p.Id);
            var bookingLegPassengers = await _dbContext.BookingLegPassengers
                .Include(lp => lp.BookingLeg)
                .Include(lp => lp.BookingPassenger)
                .Where(lp => !lp.IsDeleted && lp.BookingLeg.BookingId == bookingId && !lp.BookingLeg.IsDeleted)
                .OrderBy(lp => lp.BookingLeg.LegType)
                .ThenBy(lp => lp.BookingPassengerId)
                .ToListAsync();

            if (bookingLegPassengers.Count == 0)
            {
                throw new ValidationException("No booking legs found for ticket issuance");
            }

            var passengerServiceMap = await BuildPassengerServiceMapAsync(passengers.Select(p => p.Id).ToList());
            var tickets = new List<TicketResponse>();
            var existingTicketKeys = await _dbContext.Tickets
                .Where(t => t.BookingId == bookingId && !t.IsDeleted)
                .Select(t => new { t.BookingPassengerId, t.FlightId })
                .ToListAsync();
            var issuedKeys = existingTicketKeys
                .Select(k => $"{k.BookingPassengerId}:{k.FlightId}")
                .ToHashSet(StringComparer.Ordinal);

            var legPassengerIds = bookingLegPassengers.Select(lp => lp.Id).ToList();
            var serviceTotalsByLegPassenger = await _dbContext.BookingServices
                .Where(bs => !bs.IsDeleted
                    && bs.BookingLegPassengerId.HasValue
                    && legPassengerIds.Contains(bs.BookingLegPassengerId.Value))
                .GroupBy(bs => bs.BookingLegPassengerId!.Value)
                .Select(g => new { BookingLegPassengerId = g.Key, Total = g.Sum(x => x.Price * x.Quantity) })
                .ToDictionaryAsync(x => x.BookingLegPassengerId, x => x.Total);

            var ticketGrossByKey = new Dictionary<string, decimal>(StringComparer.Ordinal);
            decimal totalGross = 0m;
            foreach (var lp in bookingLegPassengers)
            {
                var fare = ResolveLegFare(lp, lp.BookingPassenger);
                var servicesTotal = serviceTotalsByLegPassenger.GetValueOrDefault(lp.Id, 0m);
                var gross = fare + servicesTotal;
                var key = $"{lp.BookingPassengerId}:{lp.BookingLeg.FlightId}";
                ticketGrossByKey[key] = gross;
                totalGross += gross;
            }

            var discountPool = booking.DiscountAmount > 0 ? booking.DiscountAmount : 0m;
            var allocatedDiscount = 0m;
            var issuedCount = 0;
            var expectedToIssue = bookingLegPassengers
                .Count(lp => !issuedKeys.Contains($"{lp.BookingPassengerId}:{lp.BookingLeg.FlightId}"));

            int sequenceNumber = 1;
            foreach (var legPassenger in bookingLegPassengers)
            {
                if (!passengerById.TryGetValue(legPassenger.BookingPassengerId, out var passenger))
                {
                    continue;
                }

                var flight = await _flightRepository.GetByIdAsync(legPassenger.BookingLeg.FlightId);
                if (flight == null)
                {
                    throw new NotFoundException("Flight not found");
                }

                var ticketKey = $"{passenger.Id}:{flight.Id}";
                if (issuedKeys.Contains(ticketKey))
                {
                    continue;
                }

                var seatInventory = await _seatInventoryRepository.GetByIdAsync(legPassenger.FlightSeatInventoryId);
                var ticketNumber = GenerateTicketNumber(booking.BookingCode, sequenceNumber++);
                var key = $"{passenger.Id}:{flight.Id}";
                var gross = ticketGrossByKey.GetValueOrDefault(key, seatInventory?.CurrentPrice ?? 0m);
                var discountShare = 0m;
                if (discountPool > 0m && totalGross > 0m && expectedToIssue > 0)
                {
                    issuedCount++;
                    if (issuedCount == expectedToIssue)
                    {
                        discountShare = discountPool - allocatedDiscount;
                    }
                    else
                    {
                        discountShare = Math.Round(discountPool * (gross / totalGross), 0, MidpointRounding.AwayFromZero);
                        allocatedDiscount += discountShare;
                    }
                }

                var ticket = new Ticket
                {
                    BookingPassengerId = passenger.Id,
                    TicketNumber = ticketNumber,
                    Status = 0, // Issued
                    IssuedAt = DateTime.UtcNow,
                    
                    BookingId = bookingId,
                    PassengerId = passenger.Id,
                    FlightId = flight.Id,
                    SeatClassId = seatInventory?.SeatClassId ?? 1,
                    Price = Math.Max(0m, gross - discountShare),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdTicket = await _ticketRepository.CreateAsync(ticket);
                issuedKeys.Add(ticketKey);
                tickets.Add(new TicketResponse
                {
                    TicketId = createdTicket.Id,
                    TicketNumber = ticketNumber,
                    BookingId = bookingId,
                    PassengerId = passenger.Id,
                    PassengerName = passenger.FullName,
                    FlightId = flight!.Id,
                    FlightNumber = flight.FlightNumber,
                    SeatNumber = "TBD",
                    Status = "Issued",
                    IssuedAt = VietnamTime.ToVietnamTime(createdTicket.IssuedAt),
                    DepartureTime = VietnamTime.ToVietnamTime(flight.DepartureTime),
                    DepartureAirport = flight.Route.DepartureAirport.Code,
                    ArrivalAirport = flight.Route.ArrivalAirport.Code,
                    Services = passengerServiceMap.GetValueOrDefault(passenger.Id, new List<TicketPassengerServiceDto>())
                });
            }

            _logger.LogInformation("Tickets created for booking {BookingId}: {Count} tickets", bookingId, tickets.Count);
            return tickets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tickets");
            throw;
        }
    }

    public async Task<TicketResponse> GetTicketAsync(string ticketNumber)
    {
        try
        {
            var ticket = await _ticketRepository.GetByTicketNumberAsync(ticketNumber);
            if (ticket == null)
            {
                throw new NotFoundException("Ticket not found");
            }

            var passenger = await _passengerRepository.GetByIdAsync(ticket.BookingPassengerId);
            if (passenger == null)
            {
                throw new NotFoundException("Passenger not found");
            }
            var booking = await _bookingRepository.GetByIdAsync(passenger!.BookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }
            var flight = await _flightRepository.GetByIdAsync(ticket.FlightId);
            if (flight == null)
            {
                throw new NotFoundException("Flight not found");
            }
            var passengerServiceMap = await BuildPassengerServiceMapAsync(new List<int> { passenger.Id });

            var statusString = ticket.Status switch
            {
                0 => "Issued",
                1 => "Used",
                2 => "Refunded",
                3 => "Cancelled",
                4 => "CancelledByUser",
                5 => "CancelledByAdmin",
                _ => "Unknown"
            };

            var seatClassForGet = await _dbContext.SeatClasses
                .FirstOrDefaultAsync(sc => sc.Id == ticket.SeatClassId && !sc.IsDeleted);

            return new TicketResponse
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                BookingId = booking.Id,
                PassengerId = passenger.Id,
                PassengerName = passenger.FullName,
                FlightId = flight.Id,
                FlightNumber = flight.FlightNumber,
                SeatNumber = "TBD",
                Status = statusString,
                IssuedAt = VietnamTime.ToVietnamTime(ticket.IssuedAt),
                DepartureTime = VietnamTime.ToVietnamTime(flight.DepartureTime),
                DepartureAirport = flight.Route.DepartureAirport.Code,
                ArrivalAirport = flight.Route.ArrivalAirport.Code,
                SeatClassId = ticket.SeatClassId,
                SeatClassName = seatClassForGet?.Name ?? string.Empty,
                SeatClassCode = seatClassForGet?.Code ?? string.Empty,
                Services = passengerServiceMap.GetValueOrDefault(passenger.Id, new List<TicketPassengerServiceDto>())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket");
            throw;
        }
    }

    public async Task<bool> ChangeTicketAsync(string ticketNumber, ChangeTicketDto dto)
    {
        try
        {
            var ticket = await _ticketRepository.GetByTicketNumberAsync(ticketNumber);
            if (ticket == null)
            {
                throw new NotFoundException("Ticket not found");
            }

            var currentFlight = await _flightRepository.GetByIdAsync(ticket.FlightId);
            if (currentFlight == null || currentFlight.IsDeleted)
            {
                throw new ValidationException("Current flight is invalid");
            }

            if (currentFlight.DepartureTime <= DateTime.UtcNow)
            {
                throw new ValidationException("Flight has departed. Booking changes are no longer allowed.");
            }

            var flight = await _flightRepository.GetByIdAsync(dto.NewFlightId);
            if (flight == null || flight.DepartureTime < DateTime.UtcNow)
            {
                throw new ValidationException("Invalid flight for ticket change");
            }

            // Update ticket
            ticket.ReplacedByTicketId = null; // Would create new ticket in production
            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation("Ticket changed: {TicketNumber}", ticketNumber);
            if (_notificationService != null)
            {
                var booking = await _bookingRepository.GetByIdAsync(ticket.BookingId);
                if (booking != null)
                {
                    await _notificationService.SendNotificationAsync(
                        booking.UserId,
                        "Ticket changed",
                        $"Ticket {ticket.TicketNumber} for booking {booking.BookingCode} has been changed.",
                        type: "IN_APP",
                        category: "TICKET",
                        relatedEntityType: "Ticket",
                        relatedEntityId: ticket.Id,
                        sendEmail: true);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing ticket");
            throw;
        }
    }

    public async Task<List<TicketResponse>> GetBookingTicketsAsync(int bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
            var passengerServiceMap = await BuildPassengerServiceMapAsync(passengers.Select(p => p.Id).ToList());
            var tickets = new List<TicketResponse>();

            foreach (var passenger in passengers)
            {
                var passengerTickets = await _ticketRepository.GetByBookingPassengerIdAsync(passenger.Id);
                foreach (var ticket in passengerTickets)
                {
                    var flight = await _flightRepository.GetByIdAsync(ticket.FlightId);
                    if (flight == null)
                    {
                        continue;
                    }
                    var statusString = ticket.Status switch
                    {
                        0 => "Issued",
                        1 => "Used",
                        2 => "Refunded",
                        3 => "Cancelled",
                        4 => "CancelledByUser",
                        5 => "CancelledByAdmin",
                        _ => "Unknown"
                    };

                    var seatClass = await _dbContext.SeatClasses
                        .FirstOrDefaultAsync(sc => sc.Id == ticket.SeatClassId && !sc.IsDeleted);

                    tickets.Add(new TicketResponse
                    {
                        TicketId = ticket.Id,
                        TicketNumber = ticket.TicketNumber,
                        BookingId = bookingId,
                        PassengerId = passenger.Id,
                        PassengerName = passenger.FullName,
                        FlightId = flight.Id,
                        FlightNumber = flight.FlightNumber,
                        SeatNumber = "TBD",
                        Status = statusString,
                        IssuedAt = VietnamTime.ToVietnamTime(ticket.IssuedAt),
                        DepartureTime = VietnamTime.ToVietnamTime(flight.DepartureTime),
                        DepartureAirport = flight.Route.DepartureAirport.Code,
                        ArrivalAirport = flight.Route.ArrivalAirport.Code,
                        SeatClassId = ticket.SeatClassId,
                        SeatClassName = seatClass?.Name ?? string.Empty,
                        SeatClassCode = seatClass?.Code ?? string.Empty,
                        Services = passengerServiceMap.GetValueOrDefault(passenger.Id, new List<TicketPassengerServiceDto>())
                    });
                }
            }

            return tickets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking tickets");
            throw;
        }
    }

    public async Task<byte[]> DownloadTicketAsync(string ticketNumber, string format = "pdf")
    {
        try
        {
            var ticket = await GetTicketAsync(ticketNumber);
            var html = GenerateHtmlTicket(ticket);
            return System.Text.Encoding.UTF8.GetBytes(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading ticket");
            throw;
        }
    }

    private string GenerateTicketNumber(string bookingCode, int passengerSequence)
    {
        return $"FL-{bookingCode}-{passengerSequence:D3}";
    }

    private static decimal ResolveLegFare(BookingLegPassenger legPassenger, BookingPassenger passenger)
    {
        var snapshotFare = TryReadFareFromSnapshot(passenger.FareSnapshot, legPassenger.BookingLeg.FlightId);
        if (snapshotFare.HasValue)
        {
            return snapshotFare.Value;
        }

        var seatPrice = legPassenger.FlightSeatInventory?.CurrentPrice ?? 0m;
        var passengerType = (PassengerType)passenger.PassengerType;
        return passengerType switch
        {
            PassengerType.Infant => 0m,
            PassengerType.Child => seatPrice * ChildFareRate,
            _ => seatPrice
        };
    }

    private static decimal? TryReadFareFromSnapshot(string? fareSnapshot, int flightId)
    {
        if (string.IsNullOrWhiteSpace(fareSnapshot))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(fareSnapshot);
            if (!doc.RootElement.TryGetProperty("legs", out var legs) || legs.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var leg in legs.EnumerateArray())
            {
                if (!leg.TryGetProperty("flightId", out var fId) || fId.GetInt32() != flightId)
                {
                    continue;
                }

                if (leg.TryGetProperty("fare", out var fare) && fare.TryGetDecimal(out var amount))
                {
                    return amount;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private async Task<Dictionary<int, List<TicketPassengerServiceDto>>> BuildPassengerServiceMapAsync(List<int> passengerIds)
    {
        if (passengerIds.Count == 0)
        {
            return new Dictionary<int, List<TicketPassengerServiceDto>>();
        }

        var services = await _dbContext.BookingServices
            .Include(bs => bs.AdditionalService)
            .Where(bs => passengerIds.Contains(bs.BookingPassengerId) && !bs.IsDeleted)
            .ToListAsync();

        return services
            .GroupBy(bs => bs.BookingPassengerId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(bs => new TicketPassengerServiceDto
                {
                    AdditionalServiceId = bs.AdditionalServiceId,
                    ServiceName = bs.AdditionalService?.ServiceName ?? string.Empty,
                    Quantity = bs.Quantity,
                    UnitPrice = bs.Price,
                    TotalPrice = bs.Price * bs.Quantity
                }).ToList());
    }

    private string GenerateHtmlTicket(TicketResponse ticket)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Flight Ticket</h1>
                <p><strong>Ticket:</strong> {ticket.TicketNumber}</p>
                <p><strong>Passenger:</strong> {ticket.PassengerName}</p>
                <p><strong>Flight:</strong> {ticket.FlightNumber}</p>
                <p><strong>Departure:</strong> {ticket.DepartureTime:yyyy-MM-dd HH:mm}</p>
                <p><strong>Route:</strong> {ticket.DepartureAirport} → {ticket.ArrivalAirport}</p>
            </body>
            </html>";
    }
}
