namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class PassengerRepository : IPassengerRepository
{
    private readonly FlightBookingDbContext _context;

    public PassengerRepository(FlightBookingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Passenger>> GetByBookingIdAsync(int bookingId)
    {
        return await _context.Passengers
            .AsNoTracking()
            .Where(p => p.BookingId == bookingId)
            .OrderBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<Passenger> AddAsync(Passenger passenger)
    {
        await _context.Passengers.AddAsync(passenger);
        return passenger;
    }

    public async Task<IEnumerable<Passenger>> AddRangeAsync(IEnumerable<Passenger> passengers)
    {
        var passengerList = passengers.ToList();
        await _context.Passengers.AddRangeAsync(passengerList);
        return passengerList;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
