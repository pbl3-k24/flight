namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface ISavedPassengerRepository
{
    Task<List<SavedPassenger>> GetByUserIdAsync(int userId);
    Task<SavedPassenger?> GetByIdAsync(int id);
    Task<SavedPassenger?> GetDefaultByUserIdAsync(int userId);
    Task<SavedPassenger> CreateAsync(SavedPassenger passenger);
    Task UpdateAsync(SavedPassenger passenger);
}

