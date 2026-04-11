using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Repositories;

public class FlightRepository(AppDbContext db) : IFlightRepository
{
    public Task<Flight?> GetByIdAsync(Guid id) =>
        db.Flights.Include(f => f.Route).ThenInclude(r => r.OriginAirport)
                  .Include(f => f.Route).ThenInclude(r => r.DestinationAirport)
                  .Include(f => f.Aircraft)
                  .Include(f => f.Inventories).ThenInclude(i => i.FareClass)
                  .Include(f => f.FarePrices).ThenInclude(p => p.FareClass)
                  .FirstOrDefaultAsync(f => f.Id == id);

    public async Task<IEnumerable<Flight>> GetAllAsync(int page, int pageSize) =>
        await db.Flights.Include(f => f.Route).ThenInclude(r => r.OriginAirport)
                        .Include(f => f.Route).ThenInclude(r => r.DestinationAirport)
                        .Include(f => f.Aircraft)
                        .OrderBy(f => f.DepartureTime)
                        .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<IEnumerable<Flight>> SearchAsync(string originCode, string destinationCode, DateOnly date, string? fareClassCode) =>
        await db.Flights
            .Include(f => f.Route).ThenInclude(r => r.OriginAirport)
            .Include(f => f.Route).ThenInclude(r => r.DestinationAirport)
            .Include(f => f.Aircraft)
            .Include(f => f.Inventories).ThenInclude(i => i.FareClass)
            .Include(f => f.FarePrices).ThenInclude(p => p.FareClass)
            .Where(f =>
                f.Route.OriginAirport.Code == originCode &&
                f.Route.DestinationAirport.Code == destinationCode &&
                f.DepartureTime.Date == date.ToDateTime(TimeOnly.MinValue) &&
                f.Status == "scheduled" &&
                (fareClassCode == null || f.Inventories.Any(i => i.FareClass.Code == fareClassCode && i.AvailableSeats > 0)))
            .ToListAsync();

    public async Task AddAsync(Flight flight) => await db.Flights.AddAsync(flight);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class RouteRepository(AppDbContext db) : IRouteRepository
{
    public Task<Domain.Entities.Route?> GetByIdAsync(Guid id) =>
        db.Routes.Include(r => r.OriginAirport).Include(r => r.DestinationAirport)
                 .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Domain.Entities.Route>> GetAllAsync() =>
        await db.Routes.Include(r => r.OriginAirport).Include(r => r.DestinationAirport).ToListAsync();

    public async Task AddAsync(Domain.Entities.Route route) => await db.Routes.AddAsync(route);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class AircraftRepository(AppDbContext db) : IAircraftRepository
{
    public Task<Aircraft?> GetByIdAsync(Guid id) => db.Aircrafts.FindAsync(id).AsTask();
    public async Task<IEnumerable<Aircraft>> GetAllAsync() => await db.Aircrafts.ToListAsync();
    public async Task AddAsync(Aircraft aircraft) => await db.Aircrafts.AddAsync(aircraft);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class FlightInventoryRepository(AppDbContext db) : IFlightInventoryRepository
{
    public Task<FlightInventory?> GetByFlightAndFareClassAsync(Guid flightId, Guid fareClassId) =>
        db.FlightInventories.Include(i => i.FareClass)
                            .FirstOrDefaultAsync(i => i.FlightId == flightId && i.FareClassId == fareClassId);

    public async Task<IEnumerable<FlightInventory>> GetByFlightAsync(Guid flightId) =>
        await db.FlightInventories.Include(i => i.FareClass)
                                  .Where(i => i.FlightId == flightId).ToListAsync();

    public async Task AddAsync(FlightInventory inventory) => await db.FlightInventories.AddAsync(inventory);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class FlightFarePriceRepository(AppDbContext db) : IFlightFarePriceRepository
{
    public Task<FlightFarePrice?> GetByFlightAndFareClassAsync(Guid flightId, Guid fareClassId) =>
        db.FlightFarePrices.Include(p => p.OverrideLogs)
                           .FirstOrDefaultAsync(p => p.FlightId == flightId && p.FareClassId == fareClassId);

    public async Task<IEnumerable<FlightFarePrice>> GetByFlightAsync(Guid flightId) =>
        await db.FlightFarePrices.Include(p => p.FareClass)
                                 .Where(p => p.FlightId == flightId).ToListAsync();

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class PriceRuleRepository(AppDbContext db) : IPriceRuleRepository
{
    public async Task<IEnumerable<PriceRule>> GetActiveRulesForRouteAsync(Guid routeId) =>
        await db.PriceRules.Where(r => r.IsActive && (r.RouteId == null || r.RouteId == routeId)).ToListAsync();

    public async Task<IEnumerable<PriceRule>> GetAllActiveAsync() =>
        await db.PriceRules.Where(r => r.IsActive).ToListAsync();

    public async Task AddAsync(PriceRule rule) => await db.PriceRules.AddAsync(rule);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
