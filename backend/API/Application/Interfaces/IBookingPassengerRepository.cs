namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IBookingPassengerRepository
{
    Task<BookingPassenger?> GetByIdAsync(int id);

    Task<List<BookingPassenger>> GetByBookingIdAsync(int bookingId);

    Task<IEnumerable<BookingPassenger>> GetAllAsync();

    Task<BookingPassenger> CreateAsync(BookingPassenger bookingPassenger);

    Task UpdateAsync(BookingPassenger bookingPassenger);

    Task DeleteAsync(int id);
}
