using FlightBooking.Application.DTOs.Booking;
using FlightBooking.Application.DTOs.Payment;

namespace FlightBooking.Application.Services.Interfaces;

public interface ICheckoutService
{
    /// <summary>
    /// Orchestrates: validate → snapshot prices → hold inventory → create booking → create payment intent.
    /// Returns a checkout session with payment URL.
    /// </summary>
    Task<CheckoutSessionDto> InitiateCheckoutAsync(CheckoutRequest request, Guid userId);

    /// <summary>Called when payment completes successfully. Confirms inventory, issues tickets, sends notifications.</summary>
    Task CompleteCheckoutAsync(Guid bookingId, string paymentTransactionRef);

    /// <summary>Called when payment fails. Releases held inventory.</summary>
    Task AbortCheckoutAsync(Guid bookingId, string reason);
}
