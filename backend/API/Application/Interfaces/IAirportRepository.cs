namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IAirportRepository
{
    Task<Airport?> GetByIdAsync(int id);

    Task<Airport?> GetByCodeAsync(string code);

    Task<IEnumerable<Airport>> GetAllAsync();

    Task<Airport> CreateAsync(Airport airport);

    Task UpdateAsync(Airport airport);

    Task DeleteAsync(int id);
}