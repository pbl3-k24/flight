namespace API.Domain.Entities;

public class Promotion
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int DiscountType { get; set; } = 0; // 0=PERCENTAGE, 1=FIXED

    public decimal DiscountValue { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<PromotionUsage> PromotionUsages { get; set; } = [];

    // Domain methods
    public bool IsValid(DateTime currentDateTime)
    {
        return IsActive && currentDateTime >= ValidFrom && currentDateTime <= ValidTo;
    }

    public bool IsAvailable() => !UsageLimit.HasValue || UsedCount < UsageLimit;

    public decimal CalculateDiscount(decimal amount)
    {
        return DiscountType == 0
            ? (amount * DiscountValue) / 100 // Percentage
            : DiscountValue; // Fixed amount
    }

    public bool CanBeUsed() => IsActive && IsAvailable();

    public void IncrementUsage()
    {
        UsedCount++;
    }
}
