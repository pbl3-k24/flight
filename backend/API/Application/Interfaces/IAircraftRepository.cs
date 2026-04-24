namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IAircraftRepository
{
    Task<Aircraft?> GetByIdAsync(int id);

    Task<Aircraft?> GetByRegistrationNumberAsync(string registrationNumber);

    Task<IEnumerable<Aircraft>> GetAllAsync();

    Task<Aircraft> CreateAsync(Aircraft aircraft);

    Task UpdateAsync(Aircraft aircraft);

    Task DeleteAsync(int id);
}