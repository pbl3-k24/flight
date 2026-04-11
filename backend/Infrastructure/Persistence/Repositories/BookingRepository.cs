using Application.Interfaces.Repositories;
using Domain.Entities.Booking;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(AppDbContext context) : base(context) { }

    public async Task<Booking?> GetByCodeAsync(string bookingCode, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(b => b.BookingCode == bookingCode && !b.IsDeleted, cancellationToken);

    public async Task<Booking?> GetWithDetailsAsync(Guid bookingId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(b => b.Passengers)
            .Include(b => b.Items)
                .ThenInclude(i => i.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.OriginAirport)
            .Include(b => b.Items)
                .ThenInclude(i => i.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DestinationAirport)
            .Include(b => b.Items)
                .ThenInclude(i => i.FareClass)
            .Include(b => b.Items)
                .ThenInclude(i => i.Ticket)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, cancellationToken);

    public async Task<IEnumerable<Booking>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(b => b.UserId == userId && !b.IsDeleted)
            .Include(b => b.Items).ThenInclude(i => i.Flight)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Booking>> GetExpiredPendingAsync(CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(b =>
                b.Status == BookingStatus.PendingPayment &&
                b.ExpiresAt.HasValue &&
                b.ExpiresAt.Value <= DateTime.UtcNow &&
                !b.IsDeleted)
            .ToListAsync(cancellationToken);
}
