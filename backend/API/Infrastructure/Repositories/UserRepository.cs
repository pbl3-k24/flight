using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Repositories;

public class UserRepository(FlightBookingDbContext dbContext) : IUserRepository
{
    public async Task<User> CreateAsync(User user)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public Task<bool> ExistsAsync(int id)
        => dbContext.Users.AnyAsync(x => x.Id == id);

    public Task<User?> GetByEmailAsync(string email)
        => dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);

    public Task<User?> GetWithRolesAsync(int id)
        => dbContext.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == id);

    public async Task UpdateAsync(User user)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
    }
}
