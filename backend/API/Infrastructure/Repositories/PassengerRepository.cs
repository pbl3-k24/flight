namespace API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;

/// <summary>
/// Repository for passenger data access operations.
/// Implements IPassengerRepository using Entity Framework Core.
/// </summary>
public class PassengerRepository : IPassengerRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<PassengerRepository> _logger;

    public PassengerRepository(FlightBookingDbContext context, ILogger<PassengerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Passenger>> GetByBookingIdAsync(int bookingId)
    {
        try
        {
            _logger.LogInformation("Fetching passengers for booking {BookingId}", bookingId);

            var passengers = await _context.Passengers
                .AsNoTracking()
                .Where(p => p.BookingId == bookingId)
                .OrderBy(p => p.Id)
                .ToListAsync();

            return passengers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching passengers for booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<Passenger> AddAsync(Passenger passenger)
    {
        try
        {
            _logger.LogInformation("Adding passenger for booking {BookingId}", passenger.BookingId);

            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();

            return passenger;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding passenger");
            throw;
        }
    }

    public async Task<IEnumerable<Passenger>> AddRangeAsync(IEnumerable<Passenger> passengers)
    {
        try
        {
            var passengerList = passengers.ToList();
            _logger.LogInformation("Adding {Count} passengers", passengerList.Count);

            _context.Passengers.AddRange(passengerList);
            await _context.SaveChangesAsync();

            return passengerList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple passengers");
            throw;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes");
            throw;
        }
    }
}
