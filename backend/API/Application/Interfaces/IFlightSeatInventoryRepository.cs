namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IFlightSeatInventoryRepository
{
    Task<FlightSeatInventory?> GetAsync(int flightId, int seatClassId);

    Task<IEnumerable<FlightSeatInventory>> GetAllForFlightAsync(int flightId);

    Task<List<FlightSeatInventory>> GetByFlightIdAsync(int flightId);

    Task<FlightSeatInventory?> GetByFlightAndSeatClassAsync(int flightId, int seatClassId);

    Task<bool> TryUpdateWithConcurrencyCheckAsync(int id, Func<FlightSeatInventory, bool> updateAction);

    Task<List<FlightSeatInventory>> GetActiveInventoriesAsync();

    Task<bool> TryHoldSeatsAtomicAsync(int id, int count);

    Task<bool> TryConfirmHeldSeatsAtomicAsync(int id, int count);

    Task<bool> TryReleaseHeldSeatsAtomicAsync(int id, int count);

    Task<bool> TryCancelSoldSeatsAtomicAsync(int id, int count);

    Task ReserveSeatsAsync(int id, int count, int version);

    Task UpdateAsync(FlightSeatInventory inventory);

    Task<FlightSeatInventory?> GetByIdAsync(int id);

    Task<IEnumerable<FlightSeatInventory>> GetAllAsync();

    Task CreateAsync(FlightSeatInventory inventory);

    Task DeleteAsync(int id);
}
