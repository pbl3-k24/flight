using API.Domain.Entities;

namespace API.Application.Interfaces;

public interface IAirportRepository
{
    Task<Airport?> GetByCodeAsync(string code);
    Task<IEnumerable<Airport>> GetAllActiveAsync();
    Task<Airport> CreateAsync(Airport airport);
}
