using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Repositories;

public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id) =>
        db.Payments.Include(p => p.Events).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Payment>> GetByBookingAsync(Guid bookingId) =>
        await db.Payments.Where(p => p.BookingId == bookingId).ToListAsync();

    public Task<Payment?> GetCompletedByBookingAsync(Guid bookingId) =>
        db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == "completed");

    public async Task AddAsync(Payment payment) => await db.Payments.AddAsync(payment);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class RefundRepository(AppDbContext db) : IRefundRepository
{
    public Task<Refund?> GetByIdAsync(Guid id) => db.Refunds.FindAsync(id).AsTask();

    public Task<Refund?> GetByIdWithPaymentAsync(Guid id) =>
        db.Refunds.Include(r => r.Payment).ThenInclude(p => p.Booking).ThenInclude(b => b.User)
                  .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Refund>> GetByPaymentAsync(Guid paymentId) =>
        await db.Refunds.Where(r => r.PaymentId == paymentId).ToListAsync();

    public async Task<IEnumerable<Refund>> GetByStatusAsync(string status) =>
        await db.Refunds.Where(r => r.Status == status).OrderBy(r => r.CreatedAt).ToListAsync();

    public async Task AddAsync(Refund refund) => await db.Refunds.AddAsync(refund);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class WalletLedgerRepository(AppDbContext db) : IWalletLedgerRepository
{
    public async Task<IEnumerable<WalletLedger>> GetByUserAsync(Guid userId) =>
        await db.WalletLedger.Where(w => w.UserId == userId).OrderByDescending(w => w.CreatedAt).ToListAsync();

    public async Task AddAsync(WalletLedger entry) => await db.WalletLedger.AddAsync(entry);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class IdempotencyKeyRepository(AppDbContext db) : IIdempotencyKeyRepository
{
    public Task<IdempotencyKey?> GetAsync(string key, string operationType) =>
        db.IdempotencyKeys.FirstOrDefaultAsync(ik => ik.Key == key && ik.OperationType == operationType);

    public async Task AddAsync(IdempotencyKey key) => await db.IdempotencyKeys.AddAsync(key);
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
