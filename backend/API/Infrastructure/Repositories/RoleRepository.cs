namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RoleRepository : IRoleRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(FlightBookingDbContext context, ILogger<RoleRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by id: {Id}", id);
            throw;
        }
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        try
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by name: {Name}", name);
            throw;
        }
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        try
        {
            return await _context.Roles.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles");
            throw;
        }
    }

    public async Task<Role> CreateAsync(Role role)
    {
        try
        {
            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            throw;
        }
    }

    public async Task UpdateAsync(Role role)
    {
        try
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var role = await GetByIdAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role");
            throw;
        }
    }
}
