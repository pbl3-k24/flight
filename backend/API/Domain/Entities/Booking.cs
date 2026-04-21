namespace API.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public string BookingCode { get; set; } = null!;

    public int UserId { get; set; }

    public int TripType { get; set; } = 0; // 0=OneWay, 1=RoundTrip

    public int OutboundFlightId { get; set; }

    public int? ReturnFlightId { get; set; }

    public int Status { get; set; } = 0; // 0=Pending, 1=Confirmed, 2=CheckedIn, 3=Cancelled

    public string ContactEmail { get; set; } = null!;

    public string? ContactPhone { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; } = 0;

    public decimal FinalAmount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? PromotionId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual Flight OutboundFlight { get; set; } = null!;

    public virtual Flight? ReturnFlight { get; set; }

    public virtual ICollection<BookingPassenger> Passengers { get; set; } = [];

    public virtual Payment? Payment { get; set; }

    public virtual Promotion? Promotion { get; set; }

    // Domain methods
    public string GenerateBookingCode()
    {
        // DEPRECATED: Use service method instead
        // This method is unsafe - kept for backward compatibility
        throw new NotSupportedException("Use BookingService.GenerateUniqueBookingCodeAsync() instead");
    }

    public bool CanCancel(DateTime currentDateTime) => Status != 3 && (ExpiresAt == null || currentDateTime < ExpiresAt);

    public void Cancel(string reason)
    {
        Status = 3; // Cancelled
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckIn()
    {
        Status = 2; // CheckedIn
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDiscount(decimal amount)
    {
        DiscountAmount = amount;
        FinalAmount = TotalAmount - DiscountAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasExpired(DateTime currentDateTime) => ExpiresAt.HasValue && currentDateTime > ExpiresAt;
}
