namespace API.Domain.Entities;

public class RefundPolicy
{
    public int Id { get; set; }
    public int SeatClassId { get; set; }
    public int HoursBeforeDeparture { get; set; }
    public decimal RefundPercent { get; set; }
    public decimal PenaltyFee { get; set; }

    public SeatClass SeatClass { get; set; } = null!;

    public decimal CalculateRefundAmount(decimal bookingAmount, int hoursBeforeDeparture)
    {
        if (!IsRefundable(hoursBeforeDeparture))
        {
            return 0;
        }

        var refundable = bookingAmount * (RefundPercent / 100m) - PenaltyFee;
        return Math.Max(Math.Round(refundable, 2), 0);
    }

    public bool IsRefundable(int hoursBeforeDeparture) => hoursBeforeDeparture >= HoursBeforeDeparture && RefundPercent > 0;
}
