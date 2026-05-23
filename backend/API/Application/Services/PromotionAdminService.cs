namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class PromotionAdminService : IPromotionAdminService
{
    private const int PercentageDiscountType = 0;
    private const int FixedDiscountType = 1;

    private readonly IPromotionRepository _promotionRepository;
    private readonly ILogger<PromotionAdminService> _logger;
    private readonly INotificationService? _notificationService;

    public PromotionAdminService(
        IPromotionRepository promotionRepository,
        ILogger<PromotionAdminService> logger,
        INotificationService? notificationService = null)
    {
        _promotionRepository = promotionRepository;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<PromotionManagementResponse> CreatePromotionAsync(CreatePromotionDto dto)
    {
        try
        {
            var normalizedCode = NormalizeCode(dto.Code);
            await EnsureCodeIsUniqueAsync(normalizedCode);
            ValidatePromotionRules(
                normalizedCode,
                dto.DiscountType,
                dto.DiscountValue,
                dto.MaxDiscountAmount,
                dto.MinimumAmount,
                dto.UsageLimit,
                usedCount: 0,
                dto.ValidFrom,
                dto.ValidTo);

            var now = DateTime.UtcNow;
            var promotion = new Promotion
            {
                Code = normalizedCode,
                Description = dto.Description?.Trim(),
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinimumAmount = dto.MinimumAmount,
                UsageLimit = dto.UsageLimit,
                IsActive = true,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                CreatedAt = now,
                UpdatedAt = now
            };

            var createdPromotion = await _promotionRepository.CreateAsync(promotion);

            _logger.LogInformation("Promotion created: {Code}", createdPromotion.Code);
            await TryBroadcastPromotionAsync(createdPromotion);

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

            var wasActive = promotion.IsActive;

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var normalizedCode = NormalizeCode(dto.Code);
                if (!string.Equals(normalizedCode, promotion.Code, StringComparison.Ordinal))
                {
                    await EnsureCodeIsUniqueAsync(normalizedCode, promotion.Id);
                    promotion.Code = normalizedCode;
                }
            }

            if (dto.Description != null)
            {
                promotion.Description = dto.Description.Trim();
            }

            if (dto.DiscountType.HasValue)
            {
                promotion.DiscountType = dto.DiscountType.Value;
            }

            if (dto.DiscountValue.HasValue)
            {
                promotion.DiscountValue = dto.DiscountValue.Value;
            }

            if (dto.MaxDiscountAmount.HasValue)
            {
                promotion.MaxDiscountAmount = dto.MaxDiscountAmount.Value;
            }

            if (dto.UsageLimit.HasValue)
            {
                promotion.UsageLimit = dto.UsageLimit.Value;
            }

            if (dto.MinimumAmount.HasValue)
            {
                promotion.MinimumAmount = dto.MinimumAmount.Value;
            }

            if (dto.ValidFrom.HasValue)
            {
                promotion.ValidFrom = dto.ValidFrom.Value;
            }

            if (dto.ValidTo.HasValue)
            {
                promotion.ValidTo = dto.ValidTo.Value;
            }

            if (dto.IsActive.HasValue)
            {
                promotion.IsActive = dto.IsActive.Value;
            }

            ValidatePromotionRules(
                promotion.Code,
                promotion.DiscountType,
                promotion.DiscountValue,
                promotion.MaxDiscountAmount,
                promotion.MinimumAmount,
                promotion.UsageLimit,
                promotion.UsedCount,
                promotion.ValidFrom,
                promotion.ValidTo);

            promotion.UpdatedAt = DateTime.UtcNow;
            await _promotionRepository.UpdateAsync(promotion);

            _logger.LogInformation("Promotion updated: {PromotionId}", promotionId);
            if (!wasActive && promotion.IsActive)
            {
                await TryBroadcastPromotionAsync(promotion);
            }

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
            promotion.UpdatedAt = DateTime.UtcNow;
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
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var promotions = await _promotionRepository.GetAllAsync();
            var results = new List<PromotionManagementResponse>();

            if (promotions != null)
            {
                foreach (var promotion in promotions
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                {
                    if (promotion != null)
                    {
                        results.Add(BuildPromotionResponse(promotion));
                    }
                }
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

            if (promotions != null)
            {
                var now = DateTime.UtcNow;
                var activePromotions = promotions
                    .Where(p => p.IsValid(now) && p.IsAvailable())
                    .OrderBy(p => p.ValidTo)
                    .ToList();

                foreach (var promotion in activePromotions)
                {
                    results.Add(BuildPromotionResponse(promotion));
                }
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
            var remainingLimit = promotion.UsageLimit.HasValue
                ? Math.Max(0, promotion.UsageLimit.Value - usageCount)
                : -1;
            var percentageUsed = promotion.UsageLimit is > 0
                ? Math.Min(100, (int)((usageCount * 100) / promotion.UsageLimit.Value))
                : 0;

            return new Dictionary<string, int>
            {
                { "TotalUsage", usageCount },
                { "RemainingLimit", remainingLimit },
                { "PercentageUsed", percentageUsed }
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
        if (promotion == null)
        {
            throw new ArgumentNullException(nameof(promotion), "Promotion cannot be null");
        }

        return new PromotionManagementResponse
        {
            PromotionId = promotion.Id,
            Code = promotion.Code ?? "",
            Description = promotion.Description ?? "",
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            MaxDiscountAmount = promotion.MaxDiscountAmount,
            MinimumAmount = promotion.MinimumAmount,
            UsageLimit = promotion.UsageLimit,
            UsageCount = promotion.UsedCount,
            IsActive = promotion.IsActive,
            ValidFrom = promotion.ValidFrom,
            ValidTo = promotion.ValidTo,
            CreatedAt = promotion.CreatedAt,
            UpdatedAt = promotion.UpdatedAt
        };
    }

    private static string NormalizeCode(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationException("Promotion code is required");
        }

        if (normalized.Length > 50)
        {
            throw new ValidationException("Promotion code cannot exceed 50 characters");
        }

        return normalized;
    }

    private async Task EnsureCodeIsUniqueAsync(string normalizedCode, int? excludingPromotionId = null)
    {
        var existing = await _promotionRepository.GetByCodeAsync(normalizedCode);
        if (existing != null && existing.Id != excludingPromotionId)
        {
            throw new ValidationException("Promotion code already exists");
        }
    }

    private static void ValidatePromotionRules(
        string code,
        int discountType,
        decimal discountValue,
        decimal? maxDiscountAmount,
        decimal minimumAmount,
        int? usageLimit,
        int usedCount,
        DateTime validFrom,
        DateTime validTo)
    {
        _ = NormalizeCode(code);

        if (discountType is not PercentageDiscountType and not FixedDiscountType)
        {
            throw new ValidationException("DiscountType must be 0 (percentage) or 1 (fixed)");
        }

        if (discountValue <= 0)
        {
            throw new ValidationException("DiscountValue must be greater than 0");
        }

        if (discountType == PercentageDiscountType && discountValue > 100)
        {
            throw new ValidationException("Percentage discount cannot exceed 100");
        }

        if (maxDiscountAmount.HasValue && maxDiscountAmount.Value <= 0)
        {
            throw new ValidationException("MaxDiscountAmount must be greater than 0");
        }

        if (minimumAmount < 0)
        {
            throw new ValidationException("MinimumAmount must be greater than or equal to 0");
        }

        if (usageLimit.HasValue && usageLimit.Value <= 0)
        {
            throw new ValidationException("UsageLimit must be greater than 0");
        }

        if (usageLimit.HasValue && usageLimit.Value < usedCount)
        {
            throw new ValidationException("UsageLimit cannot be less than current usage count");
        }

        if (validFrom >= validTo)
        {
            throw new ValidationException("ValidFrom must be before ValidTo");
        }
    }

    private async Task TryBroadcastPromotionAsync(Promotion promotion)
    {
        if (_notificationService == null || !promotion.IsActive)
        {
            return;
        }

        try
        {
            await _notificationService.SendPromotionalNotificationAsync(promotion.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Promotion notification broadcast failed for promotion {PromotionId}",
                promotion.Id);
        }
    }
}
