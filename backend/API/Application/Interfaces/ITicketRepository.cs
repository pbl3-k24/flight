namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(int id);

    Task<Ticket?> GetByTicketNumberAsync(string ticketNumber);

    Task<Ticket?> GetByPassengerIdAsync(int passengerId);

    Task<IEnumerable<Ticket>> GetByBookingPassengerIdAsync(int bookingPassengerId);

    Task<IEnumerable<Ticket>> GetAllAsync();

    Task<Ticket> CreateAsync(Ticket ticket);

    Task UpdateAsync(Ticket ticket);

    Task DeleteAsync(int id);
}