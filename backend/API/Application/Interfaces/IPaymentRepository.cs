namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id);

    Task<List<Payment>> GetByBookingIdAsync(int bookingId);

    Task<IEnumerable<Payment>> GetAllAsync();

    Task<Payment> CreateAsync(Payment payment);

    Task UpdateAsync(Payment payment);

    Task DeleteAsync(int id);
}