using FlightBooking.Domain.Entities;

namespace FlightBooking.Domain.Interfaces.Repositories;

public interface ISeatTemplateRepository
{
    Task<SeatTemplate?> GetByIdAsync(Guid id);
    Task<IEnumerable<SeatTemplate>> GetByAircraftAsync(Guid aircraftId);
    Task AddRangeAsync(IEnumerable<SeatTemplate> templates);
    Task SaveChangesAsync();
}
