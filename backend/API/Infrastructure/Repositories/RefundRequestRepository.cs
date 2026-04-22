namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RefundRequestRepository : IRefundRequestRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<RefundRequestRepository> _logger;

    public RefundRequestRepository(FlightBookingDbContext context, ILogger<RefundRequestRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<RefundRequest?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.RefundRequests.FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund request by id: {Id}", id);
            throw;
        }
    }

    public async Task<List<RefundRequest>> GetByBookingIdAsync(int bookingId)
    {
        try
        {
            return await _context.RefundRequests
                .Where(r => r.BookingId == bookingId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund requests for booking: {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<IEnumerable<RefundRequest>> GetByStatusAsync(int status)
    {
        try
        {
            return await _context.RefundRequests
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund requests by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<RefundRequest>> GetAllAsync()
    {
        try
        {
            return await _context.RefundRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all refund requests");
            throw;
        }
    }

    public async Task<RefundRequest> CreateAsync(RefundRequest refundRequest)
    {
        try
        {
            await _context.RefundRequests.AddAsync(refundRequest);
            await _context.SaveChangesAsync();
            return refundRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund request");
            throw;
        }
    }

    public async Task UpdateAsync(RefundRequest refundRequest)
    {
        try
        {
            _context.RefundRequests.Update(refundRequest);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating refund request");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var refundRequest = await GetByIdAsync(id);
            if (refundRequest != null)
            {
                _context.RefundRequests.Remove(refundRequest);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting refund request");
            throw;
        }
    }
}
