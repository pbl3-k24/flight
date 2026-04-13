using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class Promotion
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public int DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public bool IsValid(DateTime currentDateTime) => IsActive && currentDateTime >= ValidFrom && currentDateTime <= ValidTo;

    public bool IsAvailable() => !UsageLimit.HasValue || UsedCount < UsageLimit.Value;

    public decimal CalculateDiscount(decimal amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var discount = DiscountType == 0 ? amount * (DiscountValue / 100m) : DiscountValue;
        return Math.Min(Math.Round(discount, 2), amount);
    }

    public bool CanBeUsed() => IsAvailable() && IsValid(DateTime.UtcNow);

    public void IncrementUsage()
    {
        if (!IsAvailable())
        {
            throw new InvalidOperationException("Promotion usage limit exceeded.");
        }

        UsedCount += 1;
    }
}
