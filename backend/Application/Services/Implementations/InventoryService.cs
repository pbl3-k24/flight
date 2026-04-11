using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class SeatTemplateService(ISeatTemplateRepository seatTemplateRepository) : ISeatTemplateService
{
    public async Task<IEnumerable<SeatTemplateDto>> GetByAircraftAsync(Guid aircraftId)
    {
        var seats = await seatTemplateRepository.GetByAircraftAsync(aircraftId);
        return seats.Select(s => new SeatTemplateDto(s.Id, s.SeatNumber, s.FareClassCode, s.SeatType));
    }

    public Task<SeatTemplateDto> CreateAsync(CreateSeatTemplateRequest request, Guid adminId)
        => throw new NotSupportedException("Use BulkCreateAsync for efficiency.");

    public async Task BulkCreateAsync(Guid aircraftId, IEnumerable<CreateSeatTemplateRequest> requests, Guid adminId)
    {
        var templates = requests.Select(r => new SeatTemplate
        {
            Id = Guid.NewGuid(),
            AircraftId = aircraftId,
            SeatNumber = r.SeatNumber,
            FareClassCode = r.FareClassCode,
            SeatType = r.SeatType,
            IsActive = true
        });
        await seatTemplateRepository.AddRangeAsync(templates);
        await seatTemplateRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid adminId)
    {
        var seat = await seatTemplateRepository.GetByIdAsync(id);
        if (seat is not null)
        {
            seat.IsActive = false;
            await seatTemplateRepository.SaveChangesAsync();
        }
    }
}

public class FlightInventoryService(
    IFlightInventoryRepository inventoryRepository,
    IAircraftRepository aircraftRepository,
    IFlightRepository flightRepository) : IFlightInventoryService
{
    public async Task<FlightInventoryDto> GetByFlightAndFareClassAsync(Guid flightId, Guid fareClassId)
    {
        var inv = await inventoryRepository.GetByFlightAndFareClassAsync(flightId, fareClassId)
            ?? throw new KeyNotFoundException($"Inventory not found for flight {flightId} / fare class {fareClassId}.");
        return new FlightInventoryDto(inv.FareClassId, inv.FareClass?.Code ?? "", inv.TotalSeats, inv.AvailableSeats, inv.HeldSeats);
    }

    public async Task<IEnumerable<FlightInventoryDto>> GetByFlightAsync(Guid flightId)
    {
        var invs = await inventoryRepository.GetByFlightAsync(flightId);
        return invs.Select(i => new FlightInventoryDto(i.FareClassId, i.FareClass?.Code ?? "", i.TotalSeats, i.AvailableSeats, i.HeldSeats));
    }

    public async Task InitializeInventoryAsync(Guid flightId, Guid adminId)
    {
        // Inventory initialization would pull seat counts from aircraft seat template
        // This is a placeholder implementation
        throw new NotImplementedException("Initialize inventory based on aircraft seat templates and fare class configuration.");
    }

    public async Task<bool> HasAvailableSeatsAsync(Guid flightId, Guid fareClassId, int count)
    {
        var inv = await inventoryRepository.GetByFlightAndFareClassAsync(flightId, fareClassId);
        return inv is not null && inv.AvailableSeats >= count;
    }
}

public class InventoryReservationService(
    IFlightInventoryRepository inventoryRepository,
    IBookingRepository bookingRepository) : IInventoryReservationService
{
    public async Task<bool> HoldSeatsAsync(Guid flightId, Guid fareClassId, int count, Guid bookingId, TimeSpan holdDuration)
    {
        var inv = await inventoryRepository.GetByFlightAndFareClassAsync(flightId, fareClassId);
        if (inv is null || inv.AvailableSeats < count) return false;

        inv.AvailableSeats -= count;
        inv.HeldSeats += count;
        inv.UpdatedAt = DateTime.UtcNow;
        await inventoryRepository.SaveChangesAsync();
        return true;
    }

    public async Task ConfirmSeatsAsync(Guid flightId, Guid fareClassId, int count, Guid bookingId)
    {
        var inv = await inventoryRepository.GetByFlightAndFareClassAsync(flightId, fareClassId);
        if (inv is null) return;

        inv.HeldSeats = Math.Max(0, inv.HeldSeats - count);
        inv.SoldSeats += count;
        inv.UpdatedAt = DateTime.UtcNow;
        await inventoryRepository.SaveChangesAsync();
    }

    public async Task ReleaseSeatsAsync(Guid flightId, Guid fareClassId, int count, Guid bookingId)
    {
        var inv = await inventoryRepository.GetByFlightAndFareClassAsync(flightId, fareClassId);
        if (inv is null) return;

        inv.HeldSeats = Math.Max(0, inv.HeldSeats - count);
        inv.AvailableSeats += count;
        inv.UpdatedAt = DateTime.UtcNow;
        await inventoryRepository.SaveChangesAsync();
    }

    public async Task ReleaseExpiredHoldsAsync()
    {
        var expiredBookings = await bookingRepository.GetExpiredPendingAsync();
        foreach (var booking in expiredBookings)
        {
            var groups = booking.Items?.GroupBy(i => (i.FlightId, i.FareClassId));
            if (groups is null) continue;
            foreach (var group in groups)
            {
                await ReleaseSeatsAsync(group.Key.FlightId, group.Key.FareClassId, group.Count(), booking.Id);
            }
        }
    }
}
