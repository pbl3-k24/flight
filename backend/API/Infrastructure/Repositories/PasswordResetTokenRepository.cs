namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<PasswordResetTokenRepository> _logger;

    public PasswordResetTokenRepository(FlightBookingDbContext context, ILogger<PasswordResetTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PasswordResetToken?> GetByCodeAsync(string code)
    {
        try
        {
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password reset token by code");
            throw;
        }
    }

    public async Task<PasswordResetToken?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password reset token by ID");
            throw;
        }
    }

    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
    {
        try
        {
            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset token created for user {UserId}", token.UserId);
            return token;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating password reset token");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var token = await GetByIdAsync(id);
            if (token == null)
            {
                _logger.LogWarning("Attempted to delete non-existent password reset token {Id}", id);
                return;
            }

            _context.PasswordResetTokens.Remove(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset token deleted {Id}", id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting password reset token");
            throw;
        }
    }

    public async Task<List<PasswordResetToken>> GetByUserIdAsync(int userId)
    {
        try
        {
            return await _context.PasswordResetTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password reset tokens for user {UserId}", userId);
            throw;
        }
    }
}
