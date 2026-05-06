namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class FlightDefinitionRepository : IFlightDefinitionRepository
{
    private readonly FlightBookingDbContext _context;

    public FlightDefinitionRepository(FlightBookingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<FlightDefinition?> GetByIdAsync(int id)
    {
        return await _context.FlightDefinitions
            .Include(fd => fd.Route)
                .ThenInclude(r => r.DepartureAirport)
            .Include(fd => fd.Route)
                .ThenInclude(r => r.ArrivalAirport)
            .Include(fd => fd.DefaultAircraft)
            .FirstOrDefaultAsync(fd => fd.Id == id);
    }

    public async Task<FlightDefinition?> GetByFlightNumberAsync(string flightNumber)
    {
        return await _context.FlightDefinitions
            .Include(fd => fd.Route)
                .ThenInclude(r => r.DepartureAirport)
            .Include(fd => fd.Route)
                .ThenInclude(r => r.ArrivalAirport)
            .Include(fd => fd.DefaultAircraft)
            .FirstOrDefaultAsync(fd => fd.FlightNumber == flightNumber);
    }

    public async Task<IEnumerable<FlightDefinition>> GetAllAsync()
    {
        return await _context.FlightDefinitions
            .Include(fd => fd.Route)
                .ThenInclude(r => r.DepartureAirport)
            .Include(fd => fd.Route)
                .ThenInclude(r => r.ArrivalAirport)
            .Include(fd => fd.DefaultAircraft)
            .OrderBy(fd => fd.FlightNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<FlightDefinition>> GetActiveAsync()
    {
        return await _context.FlightDefinitions
            .Include(fd => fd.Route)
                .ThenInclude(r => r.DepartureAirport)
            .Include(fd => fd.Route)
                .ThenInclude(r => r.ArrivalAirport)
            .Include(fd => fd.DefaultAircraft)
            .Where(fd => fd.IsActive)
            .OrderBy(fd => fd.FlightNumber)
            .ToListAsync();
    }

    public async Task<FlightDefinition> CreateAsync(FlightDefinition flightDefinition)
    {
        flightDefinition.CreatedAt = DateTime.UtcNow;
        await _context.FlightDefinitions.AddAsync(flightDefinition);
        await _context.SaveChangesAsync();
        return flightDefinition;
    }

    public async Task UpdateAsync(FlightDefinition flightDefinition)
    {
        flightDefinition.UpdatedAt = DateTime.UtcNow;
        _context.FlightDefinitions.Update(flightDefinition);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var flightDefinition = await GetByIdAsync(id);
        if (flightDefinition != null)
        {
            _context.FlightDefinitions.Remove(flightDefinition);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<FlightDefinition> FindOrCreateAsync(
        string flightNumber,
        int routeId,
        int defaultAircraftId,
        TimeOnly departureTime,
        TimeOnly arrivalTime,
        int arrivalOffsetDays = 0,
        int operatingDays = 127)
    {
        // Try to find existing
        var existing = await GetByFlightNumberAsync(flightNumber);
        if (existing != null)
        {
            return existing;
        }

        // Create new
        var newDefinition = new FlightDefinition
        {
            FlightNumber = flightNumber,
            RouteId = routeId,
            DefaultAircraftId = defaultAircraftId,
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            ArrivalOffsetDays = arrivalOffsetDays,
            OperatingDays = operatingDays,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return await CreateAsync(newDefinition);
    }
}
