using FlightBooking.Application.DTOs.Booking;

namespace FlightBooking.Application.Services.Interfaces;

public interface IBookingValidationService
{
    Task ValidateBookingRequestAsync(CreateBookingRequest request);
    Task ValidatePassengerDataAsync(IEnumerable<PassengerDto> passengers);
    Task ValidateFlightAvailabilityAsync(Guid flightId, Guid fareClassId, int count);
}
