namespace API.Application.Services;

using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(
        IPromotionRepository promotionRepository,
        ILogger<PromotionService> logger)
    {
        _promotionRepository = promotionRepository;
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
            if (promotion == null || !promotion.IsActive || promotion.ValidTo < DateTime.UtcNow)
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
            var promotion = await _promotionRepository.GetByCodeAsync(code);
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

    public async Task<bool> RecordPromotionUsageAsync(int promotionId, int bookingId, decimal discountAmount)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(promotionId);
            if (promotion == null)
            {
                return false;
            }

            promotion.IncrementUsage();
            await _promotionRepository.UpdateAsync(promotion);

            _logger.LogInformation("Recorded promotion usage for promotion {PromotionId} and booking {BookingId}", promotionId, bookingId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording promotion usage");
            return false;
        }
    }
}
