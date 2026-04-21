namespace API.Application.Services;

using API.Application.Dtos.Refund;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using RefundRequestEntity = API.Domain.Entities.RefundRequest;
using Microsoft.Extensions.Logging;

public class RefundService : IRefundService
{
    private readonly IRefundRequestRepository _refundRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        IRefundRequestRepository refundRepository,
        IBookingRepository bookingRepository,
        IFlightRepository flightRepository,
        ILogger<RefundService> logger)
    {
        _refundRepository = refundRepository;
        _bookingRepository = bookingRepository;
        _flightRepository = flightRepository;
        _logger = logger;
    }

    public async Task<RefundResponse> RequestRefundAsync(int bookingId, int userId, string reason)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
            {
                throw new UnauthorizedException("Cannot request refund for this booking");
            }

            if (booking.Status != 1) // Not Confirmed
            {
                throw new ValidationException("Only confirmed bookings can be refunded");
            }

            var refund = new RefundRequestEntity
            {
                BookingId = bookingId,
                Status = 0 // Pending
            };

            var createdRefund = await _refundRepository.CreateAsync(refund);
            _logger.LogInformation("Refund requested for booking {BookingId}", bookingId);

            return new RefundResponse
            {
                RefundId = createdRefund.Id,
                BookingId = bookingId,
                Amount = booking.FinalAmount,
                Reason = reason,
                Status = "Pending",
                RequestedAt = createdRefund.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting refund");
            throw;
        }
    }

    public async Task<bool> ProcessRefundAsync(int refundId)
    {
        try
        {
            var refund = await _refundRepository.GetByIdAsync(refundId);
            if (refund == null || refund.Status != 0)
            {
                return false;
            }

            var booking = await _bookingRepository.GetByIdAsync(refund.BookingId);
            if (booking == null)
            {
                return false;
            }

            refund.Status = 2; // Processed
            await _refundRepository.UpdateAsync(refund);

            booking.Status = 3; // Cancelled
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);

            _logger.LogInformation("Refund processed for booking {BookingId}", refund.BookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund");
            return false;
        }
    }

    public async Task<RefundResponse> GetRefundStatusAsync(int refundId)
    {
        try
        {
            var refund = await _refundRepository.GetByIdAsync(refundId);
            if (refund == null)
            {
                throw new NotFoundException("Refund not found");
            }

            var booking = await _bookingRepository.GetByIdAsync(refund.BookingId);
            var statusString = refund.Status switch
            {
                0 => "Pending",
                1 => "Approved",
                2 => "Processed",
                3 => "Rejected",
                _ => "Unknown"
            };

            return new RefundResponse
            {
                RefundId = refund.Id,
                BookingId = refund.BookingId,
                Amount = booking?.FinalAmount ?? 0,
                Status = statusString,
                RequestedAt = refund.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund status");
            throw;
        }
    }

    public async Task<List<RefundResponse>> GetRefundHistoryAsync(int bookingId)
    {
        try
        {
            var refunds = await _refundRepository.GetByBookingIdAsync(bookingId);
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            var result = new List<RefundResponse>();

            foreach (var refund in refunds)
            {
                var statusString = refund.Status switch
                {
                    0 => "Pending",
                    1 => "Approved",
                    2 => "Processed",
                    3 => "Rejected",
                    _ => "Unknown"
                };

                result.Add(new RefundResponse
                {
                    RefundId = refund.Id,
                    BookingId = refund.BookingId,
                    Amount = booking?.FinalAmount ?? 0,
                    Status = statusString,
                    RequestedAt = refund.CreatedAt
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund history");
            throw;
        }
    }
}
