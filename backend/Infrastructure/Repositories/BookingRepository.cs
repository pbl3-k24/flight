using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Repositories;

public class BookingRepository(AppDbContext db) : IBookingRepository
{
    private IQueryable<Booking> WithDetails() =>
        db.Bookings
          .Include(b => b.User)
          .Include(b => b.Passengers)
          .Include(b => b.Items).ThenInclude(i => i.Flight).ThenInclude(f => f!.Route).ThenInclude(r => r.OriginAirport)
          .Include(b => b.Items).ThenInclude(i => i.Flight).ThenInclude(f => f!.Route).ThenInclude(r => r.DestinationAirport)
          .Include(b => b.Items).ThenInclude(i => i.FareClass)
          .Include(b => b.Items).ThenInclude(i => i.Passenger)
          .Include(b => b.Items).ThenInclude(i => i.Ticket);

    public Task<Booking?> GetByIdAsync(Guid id) => db.Bookings.FindAsync(id).AsTask();
    public Task<Booking?> GetByIdWithDetailsAsync(Guid id) => WithDetails().FirstOrDefaultAsync(b => b.Id == id);
    public Task<Booking?> GetByCodeAsync(string bookingCode) => WithDetails().FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

    public async Task<IEnumerable<Booking>> GetByUserAsync(Guid userId, int page, int pageSize) =>
        await WithDetails().Where(b => b.UserId == userId)
                           .OrderByDescending(b => b.CreatedAt)
                           .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<IEnumerable<Booking>> GetAllAsync(int page, int pageSize) =>
        await WithDetails().OrderByDescending(b => b.CreatedAt)
                           .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<IEnumerable<Booking>> GetExpiredPendingAsync() =>
        await db.Bookings.Where(b => b.Status == "pending_payment" && b.ExpiresAt < DateTime.UtcNow).ToListAsync();

    public async Task<IEnumerable<Booking>> GetConfirmedByFlightAsync(Guid flightId) =>
        await WithDetails()
              .Where(b => b.Status == "confirmed" && b.Items.Any(i => i.FlightId == flightId))
              .ToListAsync();

    public Task<BookingItem?> GetItemByIdAsync(Guid itemId) =>
        db.BookingItems
          .Include(i => i.FareClass)
          .Include(i => i.Flight)
          .Include(i => i.Ticket)
          .FirstOrDefaultAsync(i => i.Id == itemId);

    public async Task AddAsync(Booking booking) => await db.Bookings.AddAsync(booking);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class TicketRepository(AppDbContext db) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(Guid id) => db.Tickets.FindAsync(id).AsTask();

    public Task<Ticket?> GetByIdWithDetailsAsync(Guid id) =>
        db.Tickets.Include(t => t.BookingItem).ThenInclude(bi => bi.Booking).ThenInclude(b => b.User)
                  .Include(t => t.BookingItem).ThenInclude(bi => bi.Flight)
                  .FirstOrDefaultAsync(t => t.Id == id);

    public Task<Ticket?> GetByNumberAsync(string ticketNumber) =>
        db.Tickets.FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

    public async Task<IEnumerable<Ticket>> GetByBookingAsync(Guid bookingId) =>
        await db.Tickets.Include(t => t.BookingItem)
                        .Where(t => t.BookingItem.BookingId == bookingId).ToListAsync();

    public async Task AddRangeAsync(IEnumerable<Ticket> tickets) => await db.Tickets.AddRangeAsync(tickets);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
