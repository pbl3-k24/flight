namespace API.Application.Interfaces;

using API.Application.Dtos.Payment;

public interface IPaymentService
{
    /// <summary>
    /// Initiates a payment for a booking.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <param name="dto">Payment details</param>
    /// <returns>Payment response with link and QR code</returns>
    Task<PaymentResponse> InitiatePaymentAsync(int bookingId, InitiatePaymentDto dto);

    /// <summary>
    /// Processes a payment callback from provider.
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="callback">Callback data from provider</param>
    /// <returns>Success indicator</returns>
    Task<bool> ProcessPaymentAsync(int paymentId, PaymentCallbackDto callback);

    /// <summary>
    /// Gets payment status.
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Current payment status</returns>
    Task<PaymentResponse> GetPaymentStatusAsync(int paymentId);

    /// <summary>
    /// Gets payment history for a booking.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>List of payment attempts</returns>
    Task<List<PaymentHistoryResponse>> GetPaymentHistoryAsync(int bookingId);
}
