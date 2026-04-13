using API.Domain.Entities;

namespace API.Application.Interfaces;

public interface IFlightSeatInventoryRepository
{
    Task<FlightSeatInventory?> GetAsync(int flightId, int seatClassId);
    Task<IEnumerable<FlightSeatInventory>> GetAllForFlightAsync(int flightId);
    Task ReserveSeatsAsync(int id, int count, int version);
}
