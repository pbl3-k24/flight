namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class PaymentRepository : IPaymentRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(FlightBookingDbContext context, ILogger<PaymentRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment by id: {Id}", id);
            throw;
        }
    }

    public async Task<List<Payment>> GetByBookingIdAsync(int bookingId)
    {
        try
        {
            return await _context.Payments
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for booking: {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        try
        {
            return await _context.Payments.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all payments");
            throw;
        }
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        try
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            throw;
        }
    }

    public async Task UpdateAsync(Payment payment)
    {
        try
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var payment = await GetByIdAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment");
            throw;
        }
    }
}
