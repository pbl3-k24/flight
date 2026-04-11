using Domain.Entities.Booking;

namespace Application.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<Booking?> GetByCodeAsync(string bookingCode, CancellationToken cancellationToken = default);
    Task<Booking?> GetWithDetailsAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetExpiredPendingAsync(CancellationToken cancellationToken = default);
}
