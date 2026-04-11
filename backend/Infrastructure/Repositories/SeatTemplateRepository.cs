using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Repositories;

public class SeatTemplateRepository(AppDbContext db) : ISeatTemplateRepository
{
    public Task<SeatTemplate?> GetByIdAsync(Guid id) => db.SeatTemplates.FindAsync(id).AsTask();

    public async Task<IEnumerable<SeatTemplate>> GetByAircraftAsync(Guid aircraftId) =>
        await db.SeatTemplates.Where(s => s.AircraftId == aircraftId && s.IsActive).ToListAsync();

    public async Task AddRangeAsync(IEnumerable<SeatTemplate> templates) =>
        await db.SeatTemplates.AddRangeAsync(templates);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
