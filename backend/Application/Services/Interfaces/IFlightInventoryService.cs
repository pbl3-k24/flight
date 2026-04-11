using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IFlightInventoryService
{
    Task<FlightInventoryDto> GetByFlightAndFareClassAsync(Guid flightId, Guid fareClassId);
    Task<IEnumerable<FlightInventoryDto>> GetByFlightAsync(Guid flightId);
    Task InitializeInventoryAsync(Guid flightId, Guid adminId);
    Task<bool> HasAvailableSeatsAsync(Guid flightId, Guid fareClassId, int count);
}
