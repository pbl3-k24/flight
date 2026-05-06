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
    /// Gets payment status with ownership check.
    /// Only the payment owner or admin can view payment details.
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="userId">Current user ID for ownership check</param>
    /// <param name="isAdmin">Whether user is admin (bypasses ownership check)</param>
    /// <returns>Current payment status</returns>
    /// <exception cref="UnauthorizedException">Thrown if user is not owner and not admin</exception>
    Task<PaymentResponse> GetPaymentStatusAsync(int paymentId, int userId, bool isAdmin = false);

    /// <summary>
    /// Gets payment history for a booking with ownership check.
    /// Only the booking owner or admin can view payment history.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <param name="userId">Current user ID for ownership check</param>
    /// <param name="isAdmin">Whether user is admin (bypasses ownership check)</param>
    /// <returns>List of payment attempts</returns>
    /// <exception cref="UnauthorizedException">Thrown if user is not owner and not admin</exception>
    Task<List<PaymentHistoryResponse>> GetPaymentHistoryAsync(int bookingId, int userId, bool isAdmin = false);

    /// <summary>
    /// Processes VNPAY callback by finding payment via TransactionRef.
    /// </summary>
    Task<bool> ProcessVnpayCallbackAsync(PaymentCallbackDto callback);
}
