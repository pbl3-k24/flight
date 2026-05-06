namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class FlightScheduleTemplateRepository : IFlightScheduleTemplateRepository
{
    private readonly FlightBookingDbContext _context;

    public FlightScheduleTemplateRepository(FlightBookingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<FlightScheduleTemplate?> GetByIdAsync(int id)
    {
        return await _context.FlightScheduleTemplates
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<FlightScheduleTemplate?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.FlightScheduleTemplates
            .Include(t => t.Details)
                .ThenInclude(d => d.Route)
                    .ThenInclude(r => r.DepartureAirport)
            .Include(t => t.Details)
                .ThenInclude(d => d.Route)
                    .ThenInclude(r => r.ArrivalAirport)
            .Include(t => t.Details)
                .ThenInclude(d => d.Aircraft)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<FlightScheduleTemplate>> GetAllAsync()
    {
        return await _context.FlightScheduleTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<FlightScheduleTemplate>> GetAllWithDetailsAsync()
    {
        return await _context.FlightScheduleTemplates
            .Include(t => t.Details)
                .ThenInclude(d => d.Route)
                    .ThenInclude(r => r.DepartureAirport)
            .Include(t => t.Details)
                .ThenInclude(d => d.Route)
                    .ThenInclude(r => r.ArrivalAirport)
            .Include(t => t.Details)
                .ThenInclude(d => d.Aircraft)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<FlightScheduleTemplate> CreateAsync(FlightScheduleTemplate template)
    {
        await _context.FlightScheduleTemplates.AddAsync(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task UpdateAsync(FlightScheduleTemplate template)
    {
        _context.FlightScheduleTemplates.Update(template);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var template = await GetByIdAsync(id);
        if (template != null)
        {
            _context.FlightScheduleTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }
}
