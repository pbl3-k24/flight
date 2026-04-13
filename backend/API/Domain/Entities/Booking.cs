using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    [MaxLength(10)]
    public string BookingCode { get; set; } = string.Empty;

    public int UserId { get; set; }
    public int TripType { get; set; }
    public int OutboundFlightId { get; set; }
    public int? ReturnFlightId { get; set; }
    public int? PromotionId { get; set; }
    public int Status { get; set; }

    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Flight OutboundFlight { get; set; } = null!;
    public Flight? ReturnFlight { get; set; }
    public Promotion? Promotion { get; set; }
    public ICollection<BookingPassenger> Passengers { get; set; } = new List<BookingPassenger>();
    public Payment? Payment { get; set; }
    public ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();
    public ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();

    public string GenerateBookingCode() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    public bool CanCancel(DateTime currentDateTime)
        => Status is 0 or 1 && OutboundFlight.DepartureTime > currentDateTime;

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = 3;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckIn()
    {
        if (Status != 1)
        {
            throw new InvalidOperationException("Only confirmed bookings can check in.");
        }

        Status = 2;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDiscount(decimal amount)
    {
        if (amount < 0 || amount > TotalAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        DiscountAmount = amount;
        FinalAmount = TotalAmount - DiscountAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasExpired(DateTime currentDateTime) => ExpiresAt.HasValue && currentDateTime >= ExpiresAt.Value;
}
