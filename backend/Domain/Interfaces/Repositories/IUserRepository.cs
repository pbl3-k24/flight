using FlightBooking.Domain.Entities;

namespace FlightBooking.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByIdWithProfileAsync(Guid id);
    Task<User?> GetByIdWithRolesAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllWithProfileAsync(int page, int pageSize);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
}

public interface IOtpTokenRepository
{
    Task<OtpToken?> GetActiveAsync(Guid userId, string purpose);
    Task InvalidateExistingAsync(Guid userId, string purpose);
    Task AddAsync(OtpToken token);
    Task SaveChangesAsync();
}
