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

    // Audit properties
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Concurrency token
    public int Version { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<PromotionUsage> PromotionUsages { get; set; } = [];

    // Domain methods
    public bool IsValid(DateTime currentDateTime)
    {
        return IsActive && !IsDeleted && currentDateTime >= ValidFrom && currentDateTime <= ValidTo;
    }

    public bool IsAvailable() => !UsageLimit.HasValue || UsedCount < UsageLimit;

    public decimal CalculateDiscount(decimal amount)
    {
        return DiscountType == 0
            ? (amount * DiscountValue) / 100 // Percentage
            : DiscountValue; // Fixed amount
    }

    public bool CanBeUsed() => IsActive && !IsDeleted && IsAvailable();

    public void IncrementUsage()
    {
        UsedCount++;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}
