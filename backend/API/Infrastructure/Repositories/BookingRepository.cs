namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class BookingRepository : IBookingRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(FlightBookingDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Passengers)
                .Include(b => b.OutboundFlight)
                .Include(b => b.ReturnFlight)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking by id: {Id}", id);
            throw;
        }
    }

    public async Task<Booking?> GetByBookingCodeAsync(string code)
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.BookingCode == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking by code: {Code}", code);
            throw;
        }
    }

    public async Task<IEnumerable<Booking>> GetByUserAsync(int userId, int page, int pageSize)
    {
        try
        {
            return await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Passengers)
                .Include(b => b.OutboundFlight)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<Booking>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        try
        {
            return await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Passengers)
                .Include(b => b.OutboundFlight)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<Booking?> GetWithPassengersAsync(int id)
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking with passengers: {Id}", id);
            throw;
        }
    }

    public async Task<List<Booking>> GetRecentBookingsForFlightAsync(int flightId, int days)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Bookings
                .Where(b => b.OutboundFlightId == flightId && b.CreatedAt >= startDate)
                .Include(b => b.Passengers)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent bookings for flight: {FlightId}", flightId);
            throw;
        }
    }

    public async Task<List<Booking>> GetExpiredPendingBookingsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.Bookings
                .Where(b => b.Status == (int)BookingStatus.Pending && b.ExpiresAt < now)
                .Include(b => b.Passengers)
                .Include(b => b.OutboundFlight)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired pending bookings");
            throw;
        }
    }

    public async Task<List<Booking>> GetExpiredPendingBookingsAsync(int flightId, int seatClassId)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.Bookings
                .Where(b => b.Status == (int)BookingStatus.Pending && b.ExpiresAt < now)
                .Where(b => b.OutboundFlightId == flightId)
                .Include(b => b.Passengers)
                .Include(b => b.OutboundFlight)
                .ThenInclude(f => f.Route)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired pending bookings for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Passengers)
                .Include(b => b.OutboundFlight)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all bookings");
            throw;
        }
    }

    public async Task<Booking> CreateAsync(Booking booking)
    {
        try
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            throw;
        }
    }

    public async Task UpdateAsync(Booking booking)
    {
        try
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var booking = await GetByIdAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting booking");
            throw;
        }
    }
}
