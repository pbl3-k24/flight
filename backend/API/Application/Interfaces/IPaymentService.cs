namespace API.Application.Interfaces;

/// <summary>
/// Service interface for payment operations.
/// Handles payment processing, refunds, and payment-related transactions.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a refund for a cancelled booking.
    /// </summary>
    /// <param name="bookingId">The booking ID to refund.</param>
    /// <param name="refundAmount">The amount to refund.</param>
    /// <param name="reason">The reason for refund (e.g., "Cancellation", "Penalty applied").</param>
    /// <returns>True if refund was initiated successfully, false otherwise.</returns>
    Task<bool> ProcessRefundAsync(int bookingId, decimal refundAmount, string reason);

    /// <summary>
    /// Gets the refund percentage based on time until departure.
    /// </summary>
    /// <param name="hoursUntilDeparture">Hours until flight departure.</param>
    /// <returns>Refund percentage (0.0 to 1.0).</returns>
    decimal GetRefundPercentage(int hoursUntilDeparture);

    /// <summary>
    /// Calculates refund amount with applicable penalties.
    /// </summary>
    /// <param name="originalAmount">Original booking amount.</param>
    /// <param name="hoursUntilDeparture">Hours until flight departure.</param>
    /// <param name="cancellationFeePercentage">Cancellation fee as percentage (e.g., 0.20 for 20%).</param>
    /// <returns>Net refund amount after penalties.</returns>
    decimal CalculateRefundAmount(decimal originalAmount, int hoursUntilDeparture, decimal cancellationFeePercentage = 0.20m);
}
