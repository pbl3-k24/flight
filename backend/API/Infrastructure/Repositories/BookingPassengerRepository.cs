namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class BookingPassengerRepository : IBookingPassengerRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<BookingPassengerRepository> _logger;

    public BookingPassengerRepository(FlightBookingDbContext context, ILogger<BookingPassengerRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<BookingPassenger?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.BookingPassengers
                .FirstOrDefaultAsync(bp => bp.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking passenger by id: {Id}", id);
            throw;
        }
    }

    public async Task<List<BookingPassenger>> GetByBookingIdAsync(int bookingId)
    {
        try
        {
            return await _context.BookingPassengers
                .Where(bp => bp.BookingId == bookingId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking passengers for booking: {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<IEnumerable<BookingPassenger>> GetAllAsync()
    {
        try
        {
            return await _context.BookingPassengers
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all booking passengers");
            throw;
        }
    }

    public async Task<BookingPassenger> CreateAsync(BookingPassenger bookingPassenger)
    {
        try
        {
            await _context.BookingPassengers.AddAsync(bookingPassenger);
            await _context.SaveChangesAsync();
            return bookingPassenger;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking passenger");
            throw;
        }
    }

    public async Task UpdateAsync(BookingPassenger bookingPassenger)
    {
        try
        {
            _context.BookingPassengers.Update(bookingPassenger);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking passenger");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var bookingPassenger = await GetByIdAsync(id);
            if (bookingPassenger != null)
            {
                _context.BookingPassengers.Remove(bookingPassenger);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting booking passenger");
            throw;
        }
    }
}
