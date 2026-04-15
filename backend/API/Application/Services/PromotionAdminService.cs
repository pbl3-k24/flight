namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class PromotionAdminService : IPromotionAdminService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly ILogger<PromotionAdminService> _logger;

    public PromotionAdminService(
        IPromotionRepository promotionRepository,
        ILogger<PromotionAdminService> logger)
    {
        _promotionRepository = promotionRepository;
        _logger = logger;
    }

    public async Task<PromotionManagementResponse> CreatePromotionAsync(CreatePromotionDto dto)
    {
        try
        {
            if (dto.ValidFrom >= dto.ValidTo)
            {
                throw new ValidationException("ValidFrom must be before ValidTo");
            }

            if (dto.ValidTo < DateTime.UtcNow)
            {
                throw new ValidationException("ValidTo must be in the future");
            }

            var promotion = new Promotion
            {
                Code = dto.Code,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                UsageLimit = dto.UsageLimit,
                IsActive = true,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                CreatedAt = DateTime.UtcNow
            };

            var createdPromotion = await _promotionRepository.CreateAsync(promotion);

            _logger.LogInformation("Promotion created: {Code}", dto.Code);

            return BuildPromotionResponse(createdPromotion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion");
            throw;
        }
    }

    public async Task<bool> UpdatePromotionAsync(int promotionId, UpdatePromotionDto dto)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(promotionId);
            if (promotion == null)
            {
                throw new NotFoundException("Promotion not found");
            }

            if (dto.DiscountValue.HasValue)
            {
                promotion.DiscountValue = dto.DiscountValue.Value;
            }

            if (dto.UsageLimit.HasValue)
            {
                promotion.UsageLimit = dto.UsageLimit.Value;
            }

            if (dto.ValidTo.HasValue)
            {
                promotion.ValidTo = dto.ValidTo.Value;
            }

            if (dto.IsActive.HasValue)
            {
                promotion.IsActive = dto.IsActive.Value;
            }

            await _promotionRepository.UpdateAsync(promotion);

            _logger.LogInformation("Promotion updated: {PromotionId}", promotionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion");
            throw;
        }
    }

    public async Task<bool> DeactivatePromotionAsync(int promotionId)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(promotionId);
            if (promotion == null)
            {
                throw new NotFoundException("Promotion not found");
            }

            promotion.IsActive = false;
            await _promotionRepository.UpdateAsync(promotion);

            _logger.LogInformation("Promotion deactivated: {PromotionId}", promotionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating promotion");
            throw;
        }
    }

    public async Task<List<PromotionManagementResponse>> GetPromotionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var promotions = await _promotionRepository.GetAllAsync();
            var results = new List<PromotionManagementResponse>();

            foreach (var promotion in promotions.Skip((page - 1) * pageSize).Take(pageSize))
            {
                results.Add(BuildPromotionResponse(promotion));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotions");
            throw;
        }
    }

    public async Task<List<PromotionManagementResponse>> GetActivePromotionsAsync()
    {
        try
        {
            var promotions = await _promotionRepository.GetAllAsync();
            var results = new List<PromotionManagementResponse>();

            var now = DateTime.UtcNow;
            var activePromotions = promotions
                .Where(p => p.IsActive && p.ValidFrom <= now && p.ValidTo >= now)
                .ToList();

            foreach (var promotion in activePromotions)
            {
                results.Add(BuildPromotionResponse(promotion));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active promotions");
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetPromotionUsageAsync(int promotionId)
    {
        try
        {
            var promotion = await _promotionRepository.GetByIdAsync(promotionId);
            if (promotion == null)
            {
                throw new NotFoundException("Promotion not found");
            }

            var usageCount = promotion.UsedCount;

            return new Dictionary<string, int>
            {
                { "TotalUsage", usageCount },
                { "RemainingLimit", (promotion.UsageLimit ?? 0) - usageCount },
                { "PercentageUsed", promotion.UsageLimit.HasValue 
                    ? (int)((usageCount * 100) / promotion.UsageLimit.Value) 
                    : 0 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promotion usage");
            throw;
        }
    }

    private PromotionManagementResponse BuildPromotionResponse(Promotion promotion)
    {
        return new PromotionManagementResponse
        {
            PromotionId = promotion.Id,
            Code = promotion.Code,
            Description = "",
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            MinimumAmount = 0,
            UsageLimit = promotion.UsageLimit,
            UsageCount = promotion.UsedCount,
            IsActive = promotion.IsActive,
            ValidFrom = promotion.ValidFrom,
            ValidTo = promotion.ValidTo,
            CreatedAt = promotion.CreatedAt
        };
    }
}
