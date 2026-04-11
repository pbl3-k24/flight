using FlightBooking.Domain.Entities;

namespace FlightBooking.Domain.Interfaces.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking?> GetByIdWithDetailsAsync(Guid id);
    Task<Booking?> GetByCodeAsync(string bookingCode);
    Task<IEnumerable<Booking>> GetByUserAsync(Guid userId, int page, int pageSize);
    Task<IEnumerable<Booking>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<Booking>> GetExpiredPendingAsync();
    Task<IEnumerable<Booking>> GetConfirmedByFlightAsync(Guid flightId);
    Task<BookingItem?> GetItemByIdAsync(Guid itemId);
    Task AddAsync(Booking booking);
    Task SaveChangesAsync();
}

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id);
    Task<Ticket?> GetByIdWithDetailsAsync(Guid id);
    Task<Ticket?> GetByNumberAsync(string ticketNumber);
    Task<IEnumerable<Ticket>> GetByBookingAsync(Guid bookingId);
    Task AddRangeAsync(IEnumerable<Ticket> tickets);
    Task SaveChangesAsync();
}
