namespace API.Application.Dtos.Promotion;

public class AvailablePromotionResponse
{
    public int PromotionId { get; set; }
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public int DiscountType { get; set; } // 0=PERCENTAGE, 1=FIXED
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinimumAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}
