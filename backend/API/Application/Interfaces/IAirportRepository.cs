namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IAirportRepository
{
    Task<Airport?> GetByCodeAsync(string code);

    Task<IEnumerable<Airport>> GetAllActiveAsync();

    Task<Airport> CreateAsync(Airport airport);

    Task UpdateAsync(Airport airport);

    Task<Airport?> GetByIdAsync(int id);

    Task<IEnumerable<Airport>> GetAllAsync();

    Task DeleteAsync(int id);
}
