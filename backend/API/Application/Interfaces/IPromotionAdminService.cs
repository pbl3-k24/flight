namespace API.Application.Interfaces;

using API.Application.Dtos.Admin;

public interface IPromotionAdminService
{
    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    Task<PromotionManagementResponse> CreatePromotionAsync(CreatePromotionDto dto);

    /// <summary>
    /// Updates a promotion.
    /// </summary>
    Task<bool> UpdatePromotionAsync(int promotionId, UpdatePromotionDto dto);

    /// <summary>
    /// Deactivates a promotion.
    /// </summary>
    Task<bool> DeactivatePromotionAsync(int promotionId);

    /// <summary>
    /// Gets all promotions.
    /// </summary>
    Task<List<PromotionManagementResponse>> GetPromotionsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets active promotions.
    /// </summary>
    Task<List<PromotionManagementResponse>> GetActivePromotionsAsync();

    /// <summary>
    /// Gets promotion usage statistics.
    /// </summary>
    Task<Dictionary<string, int>> GetPromotionUsageAsync(int promotionId);
}
