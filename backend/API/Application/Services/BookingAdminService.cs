namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using RefundRequestEntity = API.Domain.Entities.RefundRequest;
using Microsoft.Extensions.Logging;

public class BookingAdminService : IBookingAdminService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRefundRequestRepository _refundRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BookingAdminService> _logger;

    public BookingAdminService(
        IBookingRepository bookingRepository,
        IRefundRequestRepository refundRepository,
        IFlightRepository flightRepository,
        IUserRepository userRepository,
        ILogger<BookingAdminService> logger)
    {
        _bookingRepository = bookingRepository;
        _refundRepository = refundRepository;
        _flightRepository = flightRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<List<BookingManagementResponse>> SearchBookingsAsync(BookingSearchFilterDto filter)
    {
        try
        {
            var bookings = await _bookingRepository.GetAllAsync();
            var results = new List<BookingManagementResponse>();

            // Apply filters
            var filtered = bookings.AsEnumerable();

            if (!string.IsNullOrEmpty(filter.BookingCode))
            {
                filtered = filtered.Where(b => b.BookingCode.Contains(filter.BookingCode));
            }

            if (filter.Status.HasValue)
            {
                filtered = filtered.Where(b => b.Status == filter.Status.Value);
            }

            if (filter.FromDate.HasValue)
            {
                filtered = filtered.Where(b => b.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                filtered = filtered.Where(b => b.CreatedAt <= filter.ToDate.Value);
            }

            // Paginate
            var paginatedBookings = filtered
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize);

            foreach (var booking in paginatedBookings)
            {
                results.Add(await BuildBookingResponseAsync(booking));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching bookings");
            throw;
        }
    }

    public async Task<BookingManagementResponse> GetBookingAsync(int bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            return await BuildBookingResponseAsync(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking");
            throw;
        }
    }

    public async Task<bool> CancelBookingAsync(int bookingId, CancelBookingAdminDto dto)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            // Update booking status
            booking.Status = 3; // Cancelled
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);

            // Create refund if requested
            if (dto.FullRefund)
            {
                var refund = new RefundRequestEntity
                {
                    BookingId = bookingId,
                    Status = 2 // Processed
                };
                await _refundRepository.CreateAsync(refund);
            }

            _logger.LogInformation("Booking cancelled by admin: {BookingId}", bookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking");
            throw;
        }
    }

    public async Task<List<RefundManagementResponse>> GetPendingRefundsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var refunds = await _refundRepository.GetByStatusAsync(0); // Pending
            var results = new List<RefundManagementResponse>();

            foreach (var refund in refunds.Skip((page - 1) * pageSize).Take(pageSize))
            {
                results.Add(await BuildRefundResponseAsync(refund));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending refunds");
            throw;
        }
    }

    public async Task<RefundManagementResponse> GetRefundAsync(int refundId)
    {
        try
        {
            var refund = await _refundRepository.GetByIdAsync(refundId);
            if (refund == null)
            {
                throw new NotFoundException("Refund not found");
            }

            return await BuildRefundResponseAsync(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund");
            throw;
        }
    }

    public async Task<bool> ApproveRefundAsync(int refundId, ApproveRefundDto dto)
    {
        try
        {
            var refund = await _refundRepository.GetByIdAsync(refundId);
            if (refund == null)
            {
                throw new NotFoundException("Refund not found");
            }

            if (dto.Approved)
            {
                refund.Status = 1; // Approved
            }
            else
            {
                refund.Status = 3; // Rejected
            }

            await _refundRepository.UpdateAsync(refund);

            _logger.LogInformation("Refund {Status}: {RefundId}", 
                dto.Approved ? "approved" : "rejected", refundId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving refund");
            throw;
        }
    }

    private async Task<BookingManagementResponse> BuildBookingResponseAsync(Booking booking)
    {
        var user = await _userRepository.GetByIdAsync(booking.UserId);
        var outboundFlight = await _flightRepository.GetByIdAsync(booking.OutboundFlightId);
        
        var statusName = booking.Status switch
        {
            0 => "Pending",
            1 => "Confirmed",
            2 => "CheckedIn",
            3 => "Cancelled",
            _ => "Unknown"
        };

        return new BookingManagementResponse
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            UserEmail = user?.Email ?? "Unknown",
            UserName = user?.FullName ?? "Unknown",
            OutboundFlight = outboundFlight?.FlightNumber ?? "N/A",
            PassengerCount = booking.TotalAmount > 0 ? (int)(booking.TotalAmount / 100) : 0,
            Amount = booking.FinalAmount,
            BookingStatus = booking.Status,
            BookingStatusName = statusName,
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt
        };
    }

    private async Task<RefundManagementResponse> BuildRefundResponseAsync(RefundRequestEntity refund)
    {
        var booking = await _bookingRepository.GetByIdAsync(refund.BookingId);
        var user = booking != null ? await _userRepository.GetByIdAsync(booking.UserId) : null;

        var statusName = refund.Status switch
        {
            0 => "Pending",
            1 => "Approved",
            2 => "Processed",
            3 => "Rejected",
            _ => "Unknown"
        };

        return new RefundManagementResponse
        {
            RefundId = refund.Id,
            BookingCode = booking?.BookingCode ?? "N/A",
            UserEmail = user?.Email ?? "Unknown",
            BookingAmount = booking?.FinalAmount ?? 0,
            RefundAmount = booking?.FinalAmount ?? 0,
            RefundStatus = refund.Status,
            RefundStatusName = statusName,
            RequestedAt = refund.CreatedAt,
            ProcessedAt = null
        };
    }
}
