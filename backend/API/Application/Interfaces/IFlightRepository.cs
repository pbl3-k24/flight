using API.Domain.Entities;

namespace API.Application.Interfaces;

public interface IFlightRepository
{
    Task<Flight?> GetByFlightNumberAsync(string flightNumber);
    Task<IEnumerable<Flight>> SearchAsync(int departureId, int arrivalId, DateTime date);
    Task<Flight?> GetWithInventoriesAsync(int id);
    Task<Flight> CreateAsync(Flight flight);
}
