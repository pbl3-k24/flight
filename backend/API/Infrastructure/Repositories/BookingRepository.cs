namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Domain.Enums;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class BookingRepository : BaseRepository<Booking>, IBookingRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(FlightBookingDbContext context, ILogger<BookingRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<Booking?> GetByIdAsync(int id)
    {
        return await _context.Bookings
            .Include(b => b.Flight)
            .Include(b => b.User)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Include(b => b.Flight)
            .Include(b => b.Passengers)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(int userId)
    {
        return await _context.Bookings.AsNoTracking().CountAsync(b => b.UserId == userId);
    }

    public async Task<Booking?> GetByReferenceAsync(string bookingReference)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Flight)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);
    }

    public async Task<IEnumerable<Booking>> GetByFlightIdAsync(int flightId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.FlightId == flightId)
            .OrderByDescending(b => b.CreatedAt)
            .Include(b => b.User)
            .Include(b => b.Passengers)
            .ToListAsync();
    }

    public async Task<Booking?> GetWithDetailsAsync(int id)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Flight)
            .Include(b => b.User)
            .Include(b => b.Passengers)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<bool> IsReferenceUniqueAsync(string bookingReference)
    {
        return !await _context.Bookings
            .AsNoTracking()
            .AnyAsync(b => b.BookingReference == bookingReference);
    }

    public async Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .Include(b => b.Flight)
            .Include(b => b.Passengers)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
            .OrderByDescending(b => b.CreatedAt)
            .Include(b => b.Flight)
            .Include(b => b.Passengers)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetPagedAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        return await _context.Bookings
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Include(b => b.Flight)
            .Include(b => b.Passengers)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Bookings.AsNoTracking().CountAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        _logger.LogDebug("Starting booking transaction");
        return await _context.Database.BeginTransactionAsync();
    }
}
