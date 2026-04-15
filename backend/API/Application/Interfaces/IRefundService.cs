namespace API.Application.Interfaces;

using API.Application.Dtos.Refund;

public interface IRefundService
{
    /// <summary>
    /// Requests a refund for a booking.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="reason">Refund reason</param>
    /// <returns>Refund details</returns>
    Task<RefundResponse> RequestRefundAsync(int bookingId, int userId, string reason);

    /// <summary>
    /// Processes a refund request.
    /// </summary>
    /// <param name="refundId">Refund request ID</param>
    /// <returns>Success indicator</returns>
    Task<bool> ProcessRefundAsync(int refundId);

    /// <summary>
    /// Gets refund status.
    /// </summary>
    /// <param name="refundId">Refund ID</param>
    /// <returns>Refund details</returns>
    Task<RefundResponse> GetRefundStatusAsync(int refundId);

    /// <summary>
    /// Gets refund history for a booking.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>List of refunds</returns>
    Task<List<RefundResponse>> GetRefundHistoryAsync(int bookingId);
}
