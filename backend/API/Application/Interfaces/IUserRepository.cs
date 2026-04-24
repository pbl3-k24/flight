namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByEmailWithRolesAsync(string email);

    Task<User?> GetWithRolesAsync(int id);

    Task<User> CreateAsync(User user);

    Task UpdateAsync(User user);

    Task DeleteAsync(int id);

    Task<bool> ExistsAsync(int id);

    Task<bool> EmailExistsAsync(string email);

    Task<IEnumerable<User>> GetAllAsync();

    Task<User?> GetByGoogleIdAsync(string googleId);
}