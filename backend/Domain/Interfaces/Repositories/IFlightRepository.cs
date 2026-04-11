using FlightBooking.Domain.Entities;

namespace FlightBooking.Domain.Interfaces.Repositories;

public interface IFlightRepository
{
    Task<Flight?> GetByIdAsync(Guid id);
    Task<IEnumerable<Flight>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<Flight>> SearchAsync(string originCode, string destinationCode, DateOnly date, string? fareClassCode);
    Task AddAsync(Flight flight);
    Task SaveChangesAsync();
}

public interface IRouteRepository
{
    Task<Entities.Route?> GetByIdAsync(Guid id);
    Task<IEnumerable<Entities.Route>> GetAllAsync();
    Task AddAsync(Entities.Route route);
    Task SaveChangesAsync();
}

public interface IAircraftRepository
{
    Task<Aircraft?> GetByIdAsync(Guid id);
    Task<IEnumerable<Aircraft>> GetAllAsync();
    Task AddAsync(Aircraft aircraft);
    Task SaveChangesAsync();
}

public interface IFlightInventoryRepository
{
    Task<FlightInventory?> GetByFlightAndFareClassAsync(Guid flightId, Guid fareClassId);
    Task<IEnumerable<FlightInventory>> GetByFlightAsync(Guid flightId);
    Task AddAsync(FlightInventory inventory);
    Task SaveChangesAsync();
}

public interface IFlightFarePriceRepository
{
    Task<FlightFarePrice?> GetByFlightAndFareClassAsync(Guid flightId, Guid fareClassId);
    Task<IEnumerable<FlightFarePrice>> GetByFlightAsync(Guid flightId);
    Task SaveChangesAsync();
}

public interface IPriceRuleRepository
{
    Task<IEnumerable<PriceRule>> GetActiveRulesForRouteAsync(Guid routeId);
    Task<IEnumerable<PriceRule>> GetAllActiveAsync();
    Task AddAsync(PriceRule rule);
    Task SaveChangesAsync();
}
