namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id);

    Task<Role?> GetByNameAsync(string name);

    Task<IEnumerable<Role>> GetAllAsync();

    Task<Role> CreateAsync(Role role);

    Task UpdateAsync(Role role);

    Task DeleteAsync(int id);
}