namespace FlightBooking.Domain.Entities;

public class Promotion
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "percent"; // percent, fixed
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Guid>? ApplicableRouteIds { get; set; }
    public ICollection<Guid>? ApplicableFareClassIds { get; set; }
}
