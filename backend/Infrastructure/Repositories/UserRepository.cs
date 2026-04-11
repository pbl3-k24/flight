using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByIdWithProfileAsync(Guid id) =>
        db.Users.Include(u => u.Profile).Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByIdWithRolesAsync(Guid id) =>
        db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant().Trim());

    public Task<bool> ExistsByEmailAsync(string email) =>
        db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant().Trim());

    public async Task<IEnumerable<User>> GetAllWithProfileAsync(int page, int pageSize) =>
        await db.Users.Include(u => u.Profile).Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                      .OrderBy(u => u.CreatedAt)
                      .Skip((page - 1) * pageSize).Take(pageSize)
                      .ToListAsync();

    public async Task AddAsync(User user) => await db.Users.AddAsync(user);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class RoleRepository(AppDbContext db) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name) =>
        db.Roles.FirstOrDefaultAsync(r => r.Name == name);
}

public class OtpTokenRepository(AppDbContext db) : IOtpTokenRepository
{
    public Task<OtpToken?> GetActiveAsync(Guid userId, string purpose) =>
        db.OtpTokens.Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
                    .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();

    public async Task InvalidateExistingAsync(Guid userId, string purpose)
    {
        var existing = await db.OtpTokens
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();
        foreach (var otp in existing) otp.IsUsed = true;
    }

    public async Task AddAsync(OtpToken token) => await db.OtpTokens.AddAsync(token);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
