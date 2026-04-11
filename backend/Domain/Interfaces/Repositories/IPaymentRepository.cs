using FlightBooking.Domain.Entities;

namespace FlightBooking.Domain.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Payment>> GetByBookingAsync(Guid bookingId);
    Task<Payment?> GetCompletedByBookingAsync(Guid bookingId);
    Task AddAsync(Payment payment);
    Task SaveChangesAsync();
}

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(Guid id);
    Task<Refund?> GetByIdWithPaymentAsync(Guid id);
    Task<IEnumerable<Refund>> GetByPaymentAsync(Guid paymentId);
    Task<IEnumerable<Refund>> GetByStatusAsync(string status);
    Task AddAsync(Refund refund);
    Task SaveChangesAsync();
}

public interface IWalletLedgerRepository
{
    Task<IEnumerable<WalletLedger>> GetByUserAsync(Guid userId);
    Task AddAsync(WalletLedger entry);
    Task SaveChangesAsync();
}

public interface IIdempotencyKeyRepository
{
    Task<IdempotencyKey?> GetAsync(string key, string operationType);
    Task AddAsync(IdempotencyKey key);
    Task SaveChangesAsync();
}
