namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class FlightTemplateDetailRepository : IFlightTemplateDetailRepository
{
    private readonly FlightBookingDbContext _context;

    public FlightTemplateDetailRepository(FlightBookingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<FlightTemplateDetail?> GetByIdAsync(int id)
    {
        return await _context.FlightTemplateDetails
            .Include(d => d.Route)
            .Include(d => d.Aircraft)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<FlightTemplateDetail>> GetByTemplateIdAsync(int templateId)
    {
        return await _context.FlightTemplateDetails
            .Include(d => d.Route)
            .Include(d => d.Aircraft)
            .Where(d => d.TemplateId == templateId)
            .OrderBy(d => d.DayOfWeek)
            .ThenBy(d => d.DepartureTime)
            .ToListAsync();
    }

    public async Task<FlightTemplateDetail> CreateAsync(FlightTemplateDetail detail)
    {
        await _context.FlightTemplateDetails.AddAsync(detail);
        await _context.SaveChangesAsync();
        return detail;
    }

    public async Task UpdateAsync(FlightTemplateDetail detail)
    {
        _context.FlightTemplateDetails.Update(detail);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var detail = await GetByIdAsync(id);
        if (detail != null)
        {
            _context.FlightTemplateDetails.Remove(detail);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByTemplateIdAsync(int templateId)
    {
        var details = await _context.FlightTemplateDetails
            .Where(d => d.TemplateId == templateId)
            .ToListAsync();
        
        _context.FlightTemplateDetails.RemoveRange(details);
        await _context.SaveChangesAsync();
    }
}
