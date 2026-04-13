namespace API.Domain.Entities;

public class PromotionUsage
{
    public int Id { get; set; }
    public int PromotionId { get; set; }
    public int BookingId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    public Promotion Promotion { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
}
