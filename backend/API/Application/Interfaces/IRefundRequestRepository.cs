namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IRefundRequestRepository
{
    Task<RefundRequest?> GetByIdAsync(int id);

    Task<List<RefundRequest>> GetByBookingIdAsync(int bookingId);

    Task<IEnumerable<RefundRequest>> GetByStatusAsync(int status);

    Task<IEnumerable<RefundRequest>> GetAllAsync();

    Task<RefundRequest> CreateAsync(RefundRequest refundRequest);

    Task UpdateAsync(RefundRequest refundRequest);

    Task DeleteAsync(int id);
}