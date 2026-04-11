using FlightBooking.Application.DTOs.Admin;
using FlightBooking.Application.DTOs.Payment;

namespace FlightBooking.Application.Services.Interfaces;

public interface IAdminRefundService
{
    Task<IEnumerable<RefundDto>> GetPendingRefundsAsync(int page, int pageSize);
    Task<RefundDto> ApproveAsync(Guid refundId, Guid adminId);
    Task<RefundDto> RejectAsync(Guid refundId, string reason, Guid adminId);
    Task<AdminRefundStatsDto> GetRefundStatsAsync(DateOnly from, DateOnly to);
}
