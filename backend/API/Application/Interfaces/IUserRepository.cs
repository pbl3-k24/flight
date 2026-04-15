namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetWithRolesAsync(int id);

    Task<User> CreateAsync(User user);

    Task UpdateAsync(User user);

    Task<bool> ExistsAsync(int id);

    Task<User?> GetByIdAsync(int id);

    Task<IEnumerable<User>> GetAllAsync();

    Task DeleteAsync(int id);
}
