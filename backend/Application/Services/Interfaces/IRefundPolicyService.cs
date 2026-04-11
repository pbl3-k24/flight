using FlightBooking.Application.DTOs.Payment;

namespace FlightBooking.Application.Services.Interfaces;

public interface IRefundPolicyService
{
    /// <summary>Evaluate whether a booking item is eligible for refund and calculate the refund amount after deducting any cancellation fee.</summary>
    Task<RefundEligibilityDto> EvaluateEligibilityAsync(Guid bookingItemId);

    /// <summary>Calculate the fee that would be charged for cancellation at this point in time.</summary>
    Task<decimal> CalculateCancellationFeeAsync(Guid bookingItemId);
}
