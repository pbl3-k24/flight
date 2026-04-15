namespace API.Application.Interfaces;

public interface IPromotionService
{
    /// <summary>
    /// Applies a promotion to a price.
    /// </summary>
    /// <param name="basePrice">Original price</param>
    /// <param name="promotionId">Promotion ID (optional)</param>
    /// <returns>Discounted price</returns>
    Task<decimal> ApplyPromotionAsync(decimal basePrice, int? promotionId);

    /// <summary>
    /// Validates if a promotion code is valid.
    /// </summary>
    /// <param name="code">Promotion code</param>
    /// <returns>Promotion details if valid</returns>
    Task<Domain.Entities.Promotion?> ValidatePromotionCodeAsync(string code);
}
