using FlightBooking.Application.DTOs.Booking;
using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class BookingValidationService(
    IFlightRepository flightRepository,
    IFlightInventoryRepository inventoryRepository) : IBookingValidationService
{
    public async Task ValidateBookingRequestAsync(CreateBookingRequest request)
    {
        if (!request.Items.Any())
            throw new ArgumentException("Booking must have at least one item.");

        foreach (var item in request.Items)
        {
            var flight = await flightRepository.GetByIdAsync(item.FlightId)
                ?? throw new KeyNotFoundException($"Flight {item.FlightId} not found.");

            if (flight.Status != "scheduled")
                throw new InvalidOperationException($"Flight {flight.FlightNumber} is not available for booking (status: {flight.Status}).");

            if (flight.DepartureTime <= DateTime.UtcNow.AddHours(1))
                throw new InvalidOperationException($"Flight {flight.FlightNumber} departs too soon.");

            await ValidateFlightAvailabilityAsync(item.FlightId, item.FareClassId, 1);
            await ValidatePassengerDataAsync([item.Passenger]);
        }
    }

    public Task ValidatePassengerDataAsync(IEnumerable<PassengerDto> passengers)
    {
        foreach (var p in passengers)
        {
            if (string.IsNullOrWhiteSpace(p.FullName))
                throw new ArgumentException("Passenger full name is required.");

            if (string.IsNullOrWhiteSpace(p.IdentityNumber))
                throw new ArgumentException("Passenger identity number is required.");

            if (p.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ArgumentException("Passenger date of birth cannot be in the future.");
        }
        return Task.CompletedTask;
    }

    public async Task ValidateFlightAvailabilityAsync(Guid flightId, Guid fareClassId, int count)
    {
        var inv = await inventoryRepository.GetByFlightAndFareClassAsync(flightId, fareClassId)
            ?? throw new KeyNotFoundException($"No inventory found for flight {flightId} / fare class {fareClassId}.");

        if (inv.AvailableSeats < count)
            throw new InvalidOperationException($"Only {inv.AvailableSeats} seats available, requested {count}.");
    }
}
