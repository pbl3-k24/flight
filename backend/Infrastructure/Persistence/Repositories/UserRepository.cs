using Application.Interfaces.Repositories;
using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

    public async Task<User?> GetWithProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
}
