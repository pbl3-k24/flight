using Application.Interfaces.Repositories;
using Domain.Entities.Flight;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FlightRepository : Repository<Flight>, IFlightRepository
{
    public FlightRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Flight>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengerCount,
        CancellationToken cancellationToken = default)
    {
        var from = departureDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = departureDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await _dbSet
            .Include(f => f.Route)
                .ThenInclude(r => r.OriginAirport)
            .Include(f => f.Route)
                .ThenInclude(r => r.DestinationAirport)
            .Include(f => f.Inventories)
                .ThenInclude(i => i.FareClass)
            .Include(f => f.FarePrices)
            .Where(f =>
                !f.IsDeleted &&
                f.Route.OriginAirportId == originAirportId &&
                f.Route.DestinationAirportId == destinationAirportId &&
                f.DepartureTime >= from &&
                f.DepartureTime <= to &&
                f.Status == FlightStatus.Scheduled &&
                f.Inventories.Any(i => i.AvailableSeats >= passengerCount))
            .OrderBy(f => f.DepartureTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Flight?> GetWithInventoryAsync(Guid flightId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(f => f.Inventories).ThenInclude(i => i.FareClass)
            .Include(f => f.FarePrices)
            .Include(f => f.Route).ThenInclude(r => r.OriginAirport)
            .Include(f => f.Route).ThenInclude(r => r.DestinationAirport)
            .FirstOrDefaultAsync(f => f.Id == flightId && !f.IsDeleted, cancellationToken);
}
