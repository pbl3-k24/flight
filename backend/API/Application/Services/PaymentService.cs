namespace API.Application.Services;

using API.Application.Interfaces;

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ProcessRefundAsync(int bookingId, decimal refundAmount, string reason)
    {
        _logger.LogInformation(
            "Refund requested. BookingId: {BookingId}, Amount: {RefundAmount}, Reason: {Reason}",
            bookingId, refundAmount, reason);

        return Task.FromResult(true);
    }

    public decimal GetRefundPercentage(int hoursUntilDeparture)
    {
        if (hoursUntilDeparture > 72)
            return 1.0m;

        if (hoursUntilDeparture > 24)
            return 0.9m;

        return 0.8m;
    }

    public decimal CalculateRefundAmount(decimal originalAmount, int hoursUntilDeparture, decimal cancellationFeePercentage = 0.20m)
    {
        if (originalAmount <= 0)
            return 0m;

        if (cancellationFeePercentage < 0m)
            cancellationFeePercentage = 0m;

        if (cancellationFeePercentage > 1m)
            cancellationFeePercentage = 1m;

        decimal refundPercentage;
        if (hoursUntilDeparture > 72)
            refundPercentage = 1m;
        else if (hoursUntilDeparture > 24)
            refundPercentage = 1m - (cancellationFeePercentage / 2m);
        else
            refundPercentage = 1m - cancellationFeePercentage;

        var refundAmount = originalAmount * refundPercentage;

        return Math.Round(refundAmount, 2, MidpointRounding.AwayFromZero);
    }
}
