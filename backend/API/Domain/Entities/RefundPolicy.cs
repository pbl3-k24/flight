namespace API.Domain.Entities;

public class RefundPolicy
{
    public int Id { get; set; }

    public int SeatClassId { get; set; }

    public int HoursBeforeDeparture { get; set; }

    public decimal RefundPercent { get; set; }

    public decimal PenaltyFee { get; set; } = 0;

    // Navigation properties
    public virtual SeatClass SeatClass { get; set; } = null!;

    // Domain methods
    public decimal CalculateRefundAmount(decimal bookingAmount, int hoursBeforeDeparture)
    {
        if (!IsRefundable(hoursBeforeDeparture))
            return 0;

        var refundAmount = (bookingAmount * RefundPercent) / 100;
        return refundAmount - PenaltyFee;
    }

    public bool IsRefundable(int hoursBeforeDeparture) => hoursBeforeDeparture >= HoursBeforeDeparture;
}
