namespace API.Application.Services;

using API.Application.Interfaces;

/// <summary>
/// Service for payment operations.
/// Handles payment processing, refunds, and payment-related transactions.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessRefundAsync(int bookingId, decimal refundAmount, string reason)
    {
        try
        {
            _logger.LogInformation(
                "Processing refund for booking {BookingId}: Amount={Amount}, Reason={Reason}",
                bookingId, refundAmount, reason);

            // TODO: Integrate with actual payment gateway (Stripe, PayPal, etc.)
            // This is a placeholder implementation
            if (refundAmount <= 0)
            {
                _logger.LogWarning("Invalid refund amount {Amount}", refundAmount);
                return false;
            }

            // Simulate successful refund
            await Task.Delay(100); // Simulate API call

            _logger.LogInformation("Refund processed successfully for booking {BookingId}", bookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for booking {BookingId}", bookingId);
            throw;
        }
    }

    public decimal GetRefundPercentage(int hoursUntilDeparture)
    {
        try
        {
            // Refund policy based on hours until departure
            // More than 72 hours: 100% refund
            // 48-72 hours: 75% refund
            // 24-48 hours: 50% refund
            // Less than 24 hours: 0% refund
            if (hoursUntilDeparture > 72)
                return 1.0m; // 100%

            if (hoursUntilDeparture > 48)
                return 0.75m; // 75%

            if (hoursUntilDeparture > 24)
                return 0.50m; // 50%

            return 0.0m; // 0%
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating refund percentage");
            throw;
        }
    }

    public decimal CalculateRefundAmount(
        decimal originalAmount,
        int hoursUntilDeparture,
        decimal cancellationFeePercentage = 0.20m)
    {
        try
        {
            _logger.LogInformation(
                "Calculating refund: Original={Amount}, Hours={Hours}, Fee%={Fee}",
                originalAmount, hoursUntilDeparture, cancellationFeePercentage);

            var refundPercentage = GetRefundPercentage(hoursUntilDeparture);
            var refundableAmount = originalAmount * refundPercentage;
            var cancellationFee = refundableAmount * cancellationFeePercentage;
            var netRefund = refundableAmount - cancellationFee;

            _logger.LogInformation(
                "Refund calculated: RefundPercentage={Percentage}, RefundableAmount={Refundable}, Fee={Fee}, NetRefund={Net}",
                refundPercentage, refundableAmount, cancellationFee, netRefund);

            return Math.Max(0, netRefund); // Ensure non-negative
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating refund amount");
            throw;
        }
    }
}
