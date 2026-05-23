namespace API.Application.Services;

using API.Application.Dtos.Promotion;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly FlightBookingDbContext _dbContext;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(
        IPromotionRepository promotionRepository,
        FlightBookingDbContext dbContext,
        ILogger<PromotionService> logger)
    {
        _promotionRepository = promotionRepository;
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<decimal> ApplyPromotionAsync(decimal basePrice, int? promotionId)
    {
        try
        {
            if (!promotionId.HasValue)
            {
                return basePrice;
            }

            var promotion = await _promotionRepository.GetByIdAsync(promotionId.Value);
            if (promotion == null || !promotion.IsValid(DateTime.UtcNow))
            {
                return basePrice;
            }

            if (!promotion.IsAvailable())
            {
                return basePrice;
            }

            var discountAmount = promotion.CalculateDiscount(basePrice);
            var finalPrice = Math.Max(0, basePrice - discountAmount);

            _logger.LogInformation("Applied promotion {PromotionId} to price {BasePrice}: {FinalPrice}", promotionId, basePrice, finalPrice);

            return finalPrice;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error applying promotion");
            return basePrice;
        }
    }

    public async Task<Promotion?> ValidatePromotionCodeAsync(string code)
    {
        try
        {
            var normalizedCode = NormalizeCode(code);
            var promotion = await _promotionRepository.GetByCodeAsync(normalizedCode);
            if (promotion == null)
            {
                return null;
            }

            if (!promotion.IsValid(DateTime.UtcNow))
            {
                return null;
            }

            if (!promotion.IsAvailable())
            {
                return null;
            }

            return promotion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promotion code {Code}", code);
            return null;
        }
    }

    public async Task<bool> RecordPromotionUsageAsync(int promotionId, int bookingId, int userId, decimal discountAmount)
    {
        try
        {
            var promotion = await _dbContext.Promotions.FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == promotionId);
            if (promotion == null)
            {
                return false;
            }

            if (!promotion.IsValid(DateTime.UtcNow) || !promotion.IsAvailable())
            {
                return false;
            }

            var bookingAlreadyUsed = await _dbContext.PromotionUsages.AnyAsync(pu => pu.BookingId == bookingId);
            if (bookingAlreadyUsed)
            {
                return true;
            }

            var alreadyUsed = await _dbContext.PromotionUsages.AnyAsync(pu =>
                pu.PromotionId == promotionId && pu.UserId == userId);
            if (alreadyUsed)
            {
                return false;
            }

            var usage = new PromotionUsage
            {
                PromotionId = promotionId,
                BookingId = bookingId,
                UserId = userId,
                DiscountAmount = discountAmount,
                UsedAt = DateTime.UtcNow
            };

            await _dbContext.PromotionUsages.AddAsync(usage);
            promotion.IncrementUsage();
            promotion.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Recorded promotion usage for promotion {PromotionId}, booking {BookingId}, user {UserId}",
                promotionId,
                bookingId,
                userId);

            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogWarning(ex, "Duplicate promotion usage detected for promotion {PromotionId} and user {UserId}", promotionId, userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording promotion usage");
            return false;
        }
    }

    public async Task<List<AvailablePromotionResponse>> GetAvailablePromotionsAsync()
    {
        var now = DateTime.UtcNow;
        var promotions = await _promotionRepository.GetActiveAsync(now);

        return promotions
            .Where(p => !p.IsDeleted && p.IsValid(now) && p.IsAvailable())
            .OrderBy(p => p.ValidTo)
            .Select(p => new AvailablePromotionResponse
            {
                PromotionId = p.Id,
                Code = p.Code,
                Description = p.Description,
                DiscountType = p.DiscountType,
                DiscountValue = p.DiscountValue,
                MaxDiscountAmount = p.MaxDiscountAmount,
                MinimumAmount = p.MinimumAmount,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo
            })
            .ToList();
    }

    private static string NormalizeCode(string code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : code.Trim().ToUpperInvariant();
    }
}
