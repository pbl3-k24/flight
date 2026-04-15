namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IFlightSeatInventoryRepository
{
    Task<FlightSeatInventory?> GetAsync(int flightId, int seatClassId);

    Task<IEnumerable<FlightSeatInventory>> GetAllForFlightAsync(int flightId);

    Task<List<FlightSeatInventory>> GetByFlightIdAsync(int flightId);

    Task<FlightSeatInventory?> GetByFlightAndSeatClassAsync(int flightId, int seatClassId);

    Task<List<FlightSeatInventory>> GetActiveInventoriesAsync();

    Task ReserveSeatsAsync(int id, int count, int version);

    Task UpdateAsync(FlightSeatInventory inventory);

    Task<FlightSeatInventory?> GetByIdAsync(int id);

    Task<IEnumerable<FlightSeatInventory>> GetAllAsync();

    Task CreateAsync(FlightSeatInventory inventory);

    Task DeleteAsync(int id);
}
