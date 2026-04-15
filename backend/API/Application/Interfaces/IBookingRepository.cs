namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IBookingRepository
{
    Task<Booking?> GetByBookingCodeAsync(string code);

    Task<IEnumerable<Booking>> GetByUserAsync(int userId, int page, int pageSize);

    Task<List<Booking>> GetByUserIdAsync(int userId, int page, int pageSize);

    Task<Booking?> GetWithPassengersAsync(int id);

    Task<List<Booking>> GetRecentBookingsForFlightAsync(int flightId, int days);

    Task<Booking> CreateAsync(Booking booking);

    Task UpdateAsync(Booking booking);

    Task<Booking?> GetByIdAsync(int id);

    Task<IEnumerable<Booking>> GetAllAsync();

    Task DeleteAsync(int id);
}
