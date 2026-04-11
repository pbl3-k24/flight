using FlightBooking.Application.DTOs.Payment;

namespace FlightBooking.Application.Services.Interfaces;

public interface IRefundService
{
    Task<RefundDto> GetByIdAsync(Guid id);
    Task<IEnumerable<RefundDto>> GetByPaymentAsync(Guid paymentId);
    Task<IEnumerable<RefundDto>> GetPendingAsync();

    /// <summary>User or admin requests a refund. Policy is evaluated before creating the record.</summary>
    Task<RefundDto> RequestRefundAsync(RefundRequest request, Guid requestedBy);

    /// <summary>Admin approves a pending refund and triggers gateway refund.</summary>
    Task<RefundDto> ApproveRefundAsync(Guid refundId, Guid adminId);

    /// <summary>Admin rejects a refund request.</summary>
    Task<RefundDto> RejectRefundAsync(Guid refundId, string reason, Guid adminId);

    /// <summary>Process gateway callback/webhook for a refund.</summary>
    Task HandleRefundCallbackAsync(Guid refundId, bool success, string? gatewayRef);
}
