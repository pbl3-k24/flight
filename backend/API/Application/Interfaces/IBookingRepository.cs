using API.Domain.Entities;

namespace API.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByBookingCodeAsync(string code);
    Task<IEnumerable<Booking>> GetByUserAsync(int userId, int page, int pageSize);
    Task<Booking?> GetWithPassengersAsync(int id);
    Task<Booking> CreateAsync(Booking booking);
}
