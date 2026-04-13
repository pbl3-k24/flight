using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Repositories;

public class FlightRepository(FlightBookingDbContext dbContext) : IFlightRepository
{
    public async Task<Flight> CreateAsync(Flight flight)
    {
        dbContext.Flights.Add(flight);
        await dbContext.SaveChangesAsync();
        return flight;
    }

    public Task<Flight?> GetByFlightNumberAsync(string flightNumber)
        => dbContext.Flights
            .Include(x => x.Route)
            .FirstOrDefaultAsync(x => x.FlightNumber == flightNumber);

    public Task<Flight?> GetWithInventoriesAsync(int id)
        => dbContext.Flights
            .Include(x => x.SeatInventories)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IEnumerable<Flight>> SearchAsync(int departureId, int arrivalId, DateTime date)
    {
        return await dbContext.Flights
            .Include(x => x.Route)
            .Where(x => x.Route.DepartureAirportId == departureId
                        && x.Route.ArrivalAirportId == arrivalId
                        && x.DepartureTime.Date == date.Date)
            .ToListAsync();
    }
}
