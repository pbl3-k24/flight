namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<EmailVerificationTokenRepository> _logger;

    public EmailVerificationTokenRepository(FlightBookingDbContext context, ILogger<EmailVerificationTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmailVerificationToken?> GetByCodeAsync(string code)
    {
        try
        {
            return await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email verification token by code");
            throw;
        }
    }

    public async Task<EmailVerificationToken?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email verification token by ID");
            throw;
        }
    }

    public async Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token)
    {
        try
        {
            _context.EmailVerificationTokens.Add(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Email verification token created for user {UserId}", token.UserId);
            return token;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating email verification token");
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
                _logger.LogWarning("Attempted to delete non-existent email verification token {Id}", id);
                return;
            }

            _context.EmailVerificationTokens.Remove(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Email verification token deleted {Id}", id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting email verification token");
            throw;
        }
    }

    public async Task<List<EmailVerificationToken>> GetByUserIdAsync(int userId)
    {
        try
        {
            return await _context.EmailVerificationTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email verification tokens for user {UserId}", userId);
            throw;
        }
    }
}
