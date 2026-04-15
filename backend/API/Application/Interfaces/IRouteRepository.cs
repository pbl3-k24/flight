namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IRouteRepository
{
    Task<Route?> GetByIdAsync(int id);

    Task<IEnumerable<Route>> GetByAirportsAsync(int departureAirportId, int arrivalAirportId);

    Task<IEnumerable<Route>> GetAllAsync();

    Task<Route> CreateAsync(Route route);

    Task UpdateAsync(Route route);

    Task DeleteAsync(int id);
}
