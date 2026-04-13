namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
    private readonly FlightBookingDbContext _context;

    public UserRepository(FlightBookingDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Users.AsNoTracking().AnyAsync(u => u.Id == id);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
