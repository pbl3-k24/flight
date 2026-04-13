namespace API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;

/// <summary>
/// Repository for booking data access operations.
/// Implements IBookingRepository using Entity Framework Core.
/// </summary>
public class BookingRepository : BaseRepository<Booking>, IBookingRepository
{
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(FlightBookingDbContext context, ILogger<BookingRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        try
        {
            _logger.LogInformation("Fetching bookings for user {UserId} - Page: {Page}, PageSize: {PageSize}", 
                userId, page, pageSize);

            var skip = (page - 1) * pageSize;

            var bookings = await DbSet
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Include(b => b.Flight)
                    .ThenInclude(f => f!.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f!.ArrivalAirport)
                .Include(b => b.Passengers)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} bookings for user {UserId}", bookings.Count, userId);

            return bookings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bookings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetCountByUserIdAsync(int userId)
    {
        try
        {
            var count = await DbSet.AsNoTracking().Where(b => b.UserId == userId).CountAsync();
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting bookings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Booking?> GetByReferenceAsync(string bookingReference)
    {
        try
        {
            _logger.LogInformation("Fetching booking with reference {BookingReference}", bookingReference);

            var booking = await DbSet
                .AsNoTracking()
                .Include(b => b.Flight)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);

            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching booking by reference {BookingReference}", bookingReference);
            throw;
        }
    }

    public async Task<IEnumerable<Booking>> GetByFlightIdAsync(int flightId)
    {
        try
        {
            _logger.LogInformation("Fetching bookings for flight {FlightId}", flightId);

            var bookings = await DbSet
                .AsNoTracking()
                .Where(b => b.FlightId == flightId)
                .Include(b => b.Passengers)
                .Include(b => b.User)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} bookings for flight {FlightId}", bookings.Count, flightId);

            return bookings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bookings for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<Booking?> GetWithDetailsAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching booking {BookingId} with details", id);

            var booking = await DbSet
                .Include(b => b.Flight)
                    .ThenInclude(f => f!.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f!.ArrivalAirport)
                .Include(b => b.User)
                .Include(b => b.Passengers)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id);

            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching booking details for {BookingId}", id);
            throw;
        }
    }

    public async Task<bool> IsReferenceUniqueAsync(string bookingReference)
    {
        try
        {
            var exists = await DbSet.AsNoTracking().AnyAsync(b => b.BookingReference == bookingReference);
            return !exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking booking reference uniqueness");
            throw;
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        try
        {
            return await DbSet.AsNoTracking().CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total booking count");
            throw;
        }
    }

    public async Task<IEnumerable<Booking>> GetByStatusAsync(Domain.Enums.BookingStatus status)
    {
        try
        {
            _logger.LogInformation("Fetching bookings with status {Status}", status);

            var bookings = await DbSet
                .AsNoTracking()
                .Where(b => b.Status == status)
                .Include(b => b.Flight)
                .Include(b => b.Passengers)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} bookings with status {Status}", bookings.Count, status);

            return bookings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bookings by status");
            throw;
        }
    }

    public async Task<IEnumerable<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation(
                "Fetching bookings between {StartDate} and {EndDate}",
                startDate.Date, endDate.Date);

            var bookings = await DbSet
                .AsNoTracking()
                .Where(b => b.CreatedAt.Date >= startDate.Date && b.CreatedAt.Date <= endDate.Date)
                .Include(b => b.Flight)
                .Include(b => b.Passengers)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} bookings in date range", bookings.Count);

            return bookings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bookings by date range");
            throw;
        }
    }

    public async Task<IDisposable> BeginTransactionAsync()
    {
        try
        {
            _logger.LogInformation("Beginning database transaction");
            var transaction = await Context.Database.BeginTransactionAsync();
            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning database transaction");
            throw;
        }
    }
}
