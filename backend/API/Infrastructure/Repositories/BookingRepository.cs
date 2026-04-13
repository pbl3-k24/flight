using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Repositories;

public class BookingRepository(FlightBookingDbContext dbContext) : IBookingRepository
{
    public async Task<Booking> CreateAsync(Booking booking)
    {
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();
        return booking;
    }

    public Task<Booking?> GetByBookingCodeAsync(string code)
        => dbContext.Bookings.FirstOrDefaultAsync(x => x.BookingCode == code);

    public async Task<IEnumerable<Booking>> GetByUserAsync(int userId, int page, int pageSize)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);

        return await dbContext.Bookings
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();
    }

    public Task<Booking?> GetWithPassengersAsync(int id)
        => dbContext.Bookings
            .Include(x => x.Passengers)
            .FirstOrDefaultAsync(x => x.Id == id);
}
