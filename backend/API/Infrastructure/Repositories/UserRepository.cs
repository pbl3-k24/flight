namespace API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;

/// <summary>
/// Repository for user data access operations.
/// Implements IUserRepository using Entity Framework Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(FlightBookingDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching user {UserId}", id);

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Fetching user by email {Email}", email);

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user by email");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        try
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user existence");
            throw;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes");
            throw;
        }
    }
}
