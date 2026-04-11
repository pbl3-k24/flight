using FlightBooking.Application.DTOs.Payment;

namespace FlightBooking.Application.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> GetByIdAsync(Guid id);
    Task<IEnumerable<PaymentDto>> GetByBookingAsync(Guid bookingId);

    /// <summary>Create a payment intent with the payment gateway and return a redirect/deep-link URL.</summary>
    Task<PaymentInitiationDto> InitiatePaymentAsync(Guid bookingId, string gateway, string returnUrl);

    /// <summary>Query the gateway for the current status of a payment.</summary>
    Task<PaymentDto> QueryPaymentStatusAsync(Guid paymentId);
}
