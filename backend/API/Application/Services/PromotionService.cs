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

            var discountAmount = promotion.DiscountType == 0 // 0=PERCENTAGE
                ? basePrice * (promotion.DiscountValue / 100)
                : promotion.DiscountValue;

            var finalPrice = Math.Max(0, basePrice - discountAmount);

            _logger.LogInformation("Applied promotion {PromotionId} to price {BasePrice}: {FinalPrice}",
                promotionId, basePrice, finalPrice);

            return finalPrice;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error applying promotion");
            return basePrice; // Return original price on error
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

            // Check if promotion is active and not expired
            if (!promotion.IsActive || promotion.ValidTo < DateTime.UtcNow)
            {
                return null;
            }

            // Check usage limit if applicable
            if (promotion.UsageLimit.HasValue && promotion.UsageLimit > 0)
            {
                // Would need to check usage count
                // var usageCount = await _promotionUsageRepository.GetCountAsync(promotion.Id);
                // if (usageCount >= promotion.UsageLimit)
                //     return null;
            }

            return promotion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promotion code {Code}", code);
            return null;
        }
    }
}
