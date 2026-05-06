namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IFlightRepository
{
    Task<Flight?> GetByFlightNumberAsync(string flightNumber);

    Task<IEnumerable<Flight>> SearchAsync(int departureId, int arrivalId, DateTime date);

    Task<Flight?> GetWithInventoriesAsync(int id);

    Task<Flight> CreateAsync(Flight flight);

    Task UpdateAsync(Flight flight);

    Task<Flight?> GetByIdAsync(int id);

    Task<Flight?> GetByIdWithDetailsAsync(int id);

    Task<IEnumerable<Flight>> GetAllAsync();

    Task<IEnumerable<Flight>> GetFlightsByRouteAndDateAsync(int departureAirportId, int arrivalAirportId, DateTime startDate, DateTime endDate);

    Task<IEnumerable<Flight>> GetFlightsByRouteAndDateAsync(int routeId, DateTime departureDate);

    Task<bool> ExistsAsync(string flightNumber, DateTime departureTime, int routeId, int aircraftId);

    Task<IEnumerable<Flight>> GetUpcomingFlightsAsync(int days = 30);

    Task DeleteAsync(int id);
}
