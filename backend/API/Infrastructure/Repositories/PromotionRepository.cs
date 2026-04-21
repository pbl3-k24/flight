namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class PromotionRepository : IPromotionRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<PromotionRepository> _logger;

    public PromotionRepository(FlightBookingDbContext context, ILogger<PromotionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Promotion?> GetByCodeAsync(string code)
    {
        try
        {
            return await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion by code: {Code}", code);
            throw;
        }
    }

    public async Task<IEnumerable<Promotion>> GetActiveAsync(DateTime currentDateTime)
    {
        try
        {
            return await _context.Promotions
                .Where(p => p.IsActive && p.ValidFrom <= currentDateTime && p.ValidTo >= currentDateTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active promotions");
            throw;
        }
    }

    public async Task<Promotion?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Promotion>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Fetching all promotions from database");
            var result = await _context.Promotions.ToListAsync();
            _logger.LogDebug("Retrieved {Count} promotions", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all promotions");
            throw;
        }
    }

    public async Task<Promotion> CreateAsync(Promotion promotion)
    {
        try
        {
            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion");
            throw;
        }
    }

    public async Task UpdateAsync(Promotion promotion)
    {
        try
        {
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var promotion = await GetByIdAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting promotion");
            throw;
        }
    }
}
