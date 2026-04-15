namespace API.Application.Interfaces;

using API.Application.Dtos.Admin;

public interface IBookingAdminService
{
    /// <summary>
    /// Searches bookings with filters.
    /// </summary>
    Task<List<BookingManagementResponse>> SearchBookingsAsync(BookingSearchFilterDto filter);

    /// <summary>
    /// Gets booking details.
    /// </summary>
    Task<BookingManagementResponse> GetBookingAsync(int bookingId);

    /// <summary>
    /// Cancels a booking (admin override).
    /// </summary>
    Task<bool> CancelBookingAsync(int bookingId, CancelBookingAdminDto dto);

    /// <summary>
    /// Gets all pending refunds.
    /// </summary>
    Task<List<RefundManagementResponse>> GetPendingRefundsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets refund details.
    /// </summary>
    Task<RefundManagementResponse> GetRefundAsync(int refundId);

    /// <summary>
    /// Approves or rejects a refund.
    /// </summary>
    Task<bool> ApproveRefundAsync(int refundId, ApproveRefundDto dto);
}
