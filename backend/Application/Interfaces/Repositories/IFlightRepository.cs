using Domain.Entities.Flight;

namespace Application.Interfaces.Repositories;

public interface IFlightRepository : IRepository<Flight>
{
    Task<IEnumerable<Flight>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengerCount,
        CancellationToken cancellationToken = default);

    Task<Flight?> GetWithInventoryAsync(Guid flightId, CancellationToken cancellationToken = default);
}
