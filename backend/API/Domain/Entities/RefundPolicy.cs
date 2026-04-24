namespace API.Domain.Entities;

public class RefundPolicy
{
    public int Id { get; set; }

    public int SeatClassId { get; set; }

    public int HoursBeforeDeparture { get; set; }

    public decimal RefundPercent { get; set; }

    public decimal PenaltyFee { get; set; } = 0;

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual SeatClass SeatClass { get; set; } = null!;

    // Domain methods
    public decimal CalculateRefundAmount(decimal bookingAmount, int hoursBeforeDeparture)
    {
        if (!IsRefundable(hoursBeforeDeparture) || IsDeleted)
            return 0;

        var refundAmount = (bookingAmount * RefundPercent) / 100;
        return refundAmount - PenaltyFee;
    }

    public bool IsRefundable(int hoursBeforeDeparture) => hoursBeforeDeparture >= HoursBeforeDeparture && !IsDeleted;

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
