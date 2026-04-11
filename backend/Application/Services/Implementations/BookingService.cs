using FlightBooking.Application.DTOs.Booking;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class BookingService(
    IBookingRepository bookingRepository,
    IBookingValidationService validationService,
    IInventoryReservationService reservationService,
    IPricingService pricingService,
    IAuditLogService auditLogService) : IBookingService
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(15);

    public async Task<BookingDto> GetByIdAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Booking {id} not found.");
        return MapToDto(booking);
    }

    public async Task<BookingDto> GetByCodeAsync(string bookingCode)
    {
        var booking = await bookingRepository.GetByCodeAsync(bookingCode)
            ?? throw new KeyNotFoundException($"Booking {bookingCode} not found.");
        return MapToDto(booking);
    }

    public async Task<IEnumerable<BookingDto>> GetByUserAsync(Guid userId, int page, int pageSize)
    {
        var bookings = await bookingRepository.GetByUserAsync(userId, page, pageSize);
        return bookings.Select(MapToDto);
    }

    public async Task<IEnumerable<BookingDto>> GetAllAsync(int page, int pageSize)
    {
        var bookings = await bookingRepository.GetAllAsync(page, pageSize);
        return bookings.Select(MapToDto);
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, Guid userId)
    {
        await validationService.ValidateBookingRequestAsync(request);

        var bookingItems = new List<BookingItem>();
        var passengers = new List<Passenger>();
        decimal totalAmount = 0;

        // Group items by flight+fareClass to validate inventory in one batch
        var flightGroups = request.Items
            .GroupBy(i => (i.FlightId, i.FareClassId))
            .ToList();

        foreach (var group in flightGroups)
        {
            var count = group.Count();
            var held = await reservationService.HoldSeatsAsync(
                group.Key.FlightId, group.Key.FareClassId, count,
                Guid.Empty, // will be set after booking is created
                HoldDuration);

            if (!held)
                throw new InvalidOperationException($"Not enough seats available for flight {group.Key.FlightId}.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = GenerateBookingCode(),
            UserId = userId,
            Status = "pending_payment",
            Currency = "VND",
            ExpiresAt = DateTime.UtcNow.Add(HoldDuration),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var breakdown = await pricingService.CalculatePriceAsync(item.FlightId, item.FareClassId, 1);

            var passenger = new Passenger
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                FullName = item.Passenger.FullName,
                DateOfBirth = item.Passenger.DateOfBirth,
                Gender = item.Passenger.Gender,
                Nationality = item.Passenger.Nationality,
                IdentityNumber = item.Passenger.IdentityNumber,
                PassengerType = item.Passenger.PassengerType,
                PassportNumber = item.Passenger.PassportNumber,
                PassportExpiry = item.Passenger.PassportExpiry
            };
            passengers.Add(passenger);

            var bookingItem = new BookingItem
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                PassengerId = passenger.Id,
                FlightId = item.FlightId,
                FareClassId = item.FareClassId,
                SeatNumber = item.SeatNumber,
                Price = breakdown.BaseFare + breakdown.Tax,
                TaxAndFee = breakdown.ServiceFee,
                Status = "active"
            };
            bookingItems.Add(bookingItem);
            totalAmount += bookingItem.Price + bookingItem.TaxAndFee;
        }

        booking.TotalAmount = totalAmount;
        booking.Passengers = passengers;
        booking.Items = bookingItems;

        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        return MapToDto(booking);
    }

    public async Task CancelAsync(Guid id, string reason, Guid requestedBy)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Booking {id} not found.");

        if (booking.Status == "cancelled")
            throw new InvalidOperationException("Booking is already cancelled.");

        var previousStatus = booking.Status;

        // Release inventory
        foreach (var group in booking.Items.GroupBy(i => (i.FlightId, i.FareClassId)))
        {
            await reservationService.ReleaseSeatsAsync(group.Key.FlightId, group.Key.FareClassId, group.Count(), booking.Id);
        }

        booking.Status = "cancelled";
        booking.CancellationReason = reason;
        booking.UpdatedAt = DateTime.UtcNow;
        await bookingRepository.SaveChangesAsync();

        await auditLogService.LogAsync("booking_cancelled", "Booking", id.ToString(),
            new { Status = previousStatus }, new { Status = "cancelled", Reason = reason }, requestedBy);
    }

    public async Task ExpireBookingAsync(Guid id)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Booking {id} not found.");

        if (booking.Status != "pending_payment") return;

        foreach (var group in booking.Items.GroupBy(i => (i.FlightId, i.FareClassId)))
        {
            await reservationService.ReleaseSeatsAsync(group.Key.FlightId, group.Key.FareClassId, group.Count(), booking.Id);
        }

        booking.Status = "expired";
        booking.UpdatedAt = DateTime.UtcNow;
        await bookingRepository.SaveChangesAsync();
    }

    private static string GenerateBookingCode()
    {
        var prefix = "BK";
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(10000, 99999);
        return $"{prefix}{datePart}{random}";
    }

    private static BookingDto MapToDto(Booking booking) => new(
        booking.Id,
        booking.BookingCode,
        booking.UserId,
        booking.Status,
        booking.TotalAmount,
        booking.Currency,
        booking.ExpiresAt,
        booking.CreatedAt,
        booking.Items.Select(i => new BookingItemDto(
            i.Id,
            i.Flight?.FlightNumber ?? string.Empty,
            i.Flight?.Route?.OriginAirport?.Code ?? string.Empty,
            i.Flight?.Route?.DestinationAirport?.Code ?? string.Empty,
            i.Flight?.DepartureTime ?? default,
            i.Flight?.ArrivalTime ?? default,
            i.FareClass?.Code ?? string.Empty,
            i.SeatNumber,
            i.Price,
            i.TaxAndFee,
            i.Status,
            i.Ticket is null ? null : new TicketDto(
                i.Ticket.Id, i.Ticket.TicketNumber, i.Ticket.Status,
                i.Ticket.IssuedAt, i.Ticket.BoardingPassUrl, i.Ticket.ETicketUrl),
            i.Passenger?.FullName ?? string.Empty)),
        booking.Passengers.Select(p => new PassengerSummaryDto(p.Id, p.FullName, p.PassengerType, p.IdentityNumber)));
}
