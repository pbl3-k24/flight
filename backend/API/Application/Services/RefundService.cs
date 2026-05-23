namespace API.Application.Services;

using API.Application.Dtos.Refund;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RefundRequestEntity = API.Domain.Entities.RefundRequest;

public class RefundService : IRefundService
{
    private readonly IRefundRequestRepository _refundRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly FlightBookingDbContext _dbContext;
    private readonly ILogger<RefundService> _logger;
    private readonly INotificationService? _notificationService;

    public RefundService(
        IRefundRequestRepository refundRepository,
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        FlightBookingDbContext dbContext,
        ILogger<RefundService> logger,
        INotificationService? notificationService = null)
    {
        _refundRepository = refundRepository;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _dbContext = dbContext;
        _logger = logger;
        _notificationService = notificationService;
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

            if (booking.Status != (int)BookingStatus.Confirmed
                && booking.Status != (int)BookingStatus.PartiallyCancelled
                && booking.Status != (int)BookingStatus.CheckedIn)
            {
                throw new ValidationException("Booking status does not allow refund request");
            }

            var existingPendingRefund = (await _refundRepository.GetByBookingIdAsync(bookingId))
                .Any(r => !r.IsDeleted && r.Status == 0);
            if (existingPendingRefund)
            {
                throw new ValidationException("A pending refund request already exists for this booking");
            }

            var payment = (await _paymentRepository.GetByBookingIdAsync(bookingId))
                .Where(p => !p.IsDeleted &&
                            (p.Status == (int)PaymentStatus.Completed
                             || p.Status == (int)PaymentStatus.PartialRefunded))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();
            if (payment == null)
            {
                throw new ValidationException("Cannot create refund request because no refundable payment was found");
            }

            var refundableTickets = await GetRefundableTicketsAsync(bookingId);
            if (refundableTickets.Count == 0)
            {
                throw new ValidationException("No refundable tickets found for this booking");
            }

            var reasonValue = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            var createdRefundRequests = new List<RefundRequestEntity>();

            foreach (var ticket in refundableTickets)
            {
                var amount = await CalculateTicketRefundAmountAsync(bookingId, ticket);
                if (amount <= 0)
                {
                    continue;
                }

                var refund = new RefundRequestEntity
                {
                    BookingId = bookingId,
                    PaymentId = payment.Id,
                    TicketId = ticket.Id,
                    RefundAmount = amount,
                    SourceAmountSnapshot = amount,
                    Reason = reasonValue,
                    CreatedAt = DateTime.UtcNow,
                    Status = 0
                };

                createdRefundRequests.Add(refund);
            }

            if (createdRefundRequests.Count == 0)
            {
                throw new ValidationException("No refundable ticket amount found");
            }

            await _dbContext.RefundRequests.AddRangeAsync(createdRefundRequests);
            await _dbContext.SaveChangesAsync();

            var aggregateAmount = createdRefundRequests.Sum(r => r.RefundAmount);
            _logger.LogInformation(
                "Ticket-level refund requested for booking {BookingId}: {TicketCount} tickets, amount {Amount}",
                bookingId,
                createdRefundRequests.Count,
                aggregateAmount);

            return new RefundResponse
            {
                RefundId = createdRefundRequests[0].Id,
                BookingId = bookingId,
                Amount = aggregateAmount,
                Reason = reasonValue ?? string.Empty,
                Status = "Pending",
                RequestedAt = createdRefundRequests.Min(r => r.CreatedAt)
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

            refund.Status = 2;
            refund.ProcessedAt = DateTime.UtcNow;
            await _refundRepository.UpdateAsync(refund);

            var payment = await _paymentRepository.GetByIdAsync(refund.PaymentId);
            if (payment != null)
            {
                var processedAmount = await _dbContext.RefundRequests
                    .Where(r => !r.IsDeleted && r.PaymentId == payment.Id && r.Status == 2)
                    .SumAsync(r => (decimal?)r.RefundAmount) ?? 0m;

                payment.Status = processedAmount >= payment.Amount
                    ? (int)PaymentStatus.Refunded
                    : (int)PaymentStatus.PartialRefunded;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            var activeTicketCount = await _dbContext.Tickets.CountAsync(t =>
                !t.IsDeleted && t.BookingId == refund.BookingId && (t.Status == 0 || t.Status == 1 || t.Status == 2));
            if (activeTicketCount == 0)
            {
                booking.Status = (int)BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(booking);
            }

            _logger.LogInformation("Refund processed for booking {BookingId}", refund.BookingId);
            if (_notificationService != null)
            {
                await _notificationService.SendNotificationAsync(
                    booking.UserId,
                    "Refund processed",
                    $"Refund request {refund.Id} for booking {booking.BookingCode} has been processed.",
                    type: "IN_APP",
                    category: "REFUND",
                    relatedEntityType: "RefundRequest",
                    relatedEntityId: refund.Id,
                    sendEmail: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund");
            return false;
        }
    }

    public async Task<RefundResponse> GetRefundStatusAsync(int refundId, int userId, bool isAdmin = false)
    {
        try
        {
            var refund = await _refundRepository.GetByIdAsync(refundId);
            if (refund == null)
            {
                throw new NotFoundException("Refund not found");
            }

            var booking = await _bookingRepository.GetByIdAsync(refund.BookingId);
            if (booking == null)
            {
                throw new NotFoundException("Associated booking not found");
            }

            if (!isAdmin && booking.UserId != userId)
            {
                throw new UnauthorizedException("You cannot access this refund");
            }

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
                Amount = refund.RefundAmount,
                Reason = refund.Reason ?? string.Empty,
                Status = statusString,
                RequestedAt = refund.CreatedAt,
                ProcessedAt = refund.ProcessedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund status");
            throw;
        }
    }

    public async Task<List<RefundResponse>> GetRefundHistoryAsync(int bookingId, int userId, bool isAdmin = false)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            if (!isAdmin && booking.UserId != userId)
            {
                throw new UnauthorizedException("You cannot access this booking's refund history");
            }

            var refunds = await _refundRepository.GetByBookingIdAsync(bookingId);
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
                    Amount = refund.RefundAmount,
                    Reason = refund.Reason ?? string.Empty,
                    Status = statusString,
                    RequestedAt = refund.CreatedAt,
                    ProcessedAt = refund.ProcessedAt
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

    private async Task<List<Ticket>> GetRefundableTicketsAsync(int bookingId)
    {
        var issuedTickets = await _dbContext.Tickets
            .Where(t => !t.IsDeleted && t.BookingId == bookingId && t.Status == 0)
            .ToListAsync();

        if (issuedTickets.Count == 0)
        {
            return [];
        }

        var ticketIds = issuedTickets.Select(t => t.Id).ToList();
        var blockedTicketIds = await _dbContext.RefundRequests
            .Where(r => !r.IsDeleted
                        && r.TicketId.HasValue
                        && ticketIds.Contains(r.TicketId.Value)
                        && (r.Status == 0 || r.Status == 1 || r.Status == 2))
            .Select(r => r.TicketId!.Value)
            .ToListAsync();

        return issuedTickets
            .Where(t => !blockedTicketIds.Contains(t.Id))
            .ToList();
    }

    private async Task<decimal> CalculateTicketRefundAmountAsync(int bookingId, Ticket ticket)
    {
        decimal serviceAmount = 0;
        var legPassengers = await _dbContext.BookingLegPassengers
            .Where(lp => lp.BookingPassengerId == ticket.PassengerId)
            .Select(lp => lp.Id)
            .ToListAsync();

        var servicesByLeg = await _dbContext.BookingServices
            .Where(bs => !bs.IsDeleted
                && bs.BookingPassengerId == ticket.PassengerId
                && bs.BookingLegPassengerId.HasValue
                && legPassengers.Contains(bs.BookingLegPassengerId.Value))
            .ToListAsync();

        if (servicesByLeg.Count > 0)
        {
            serviceAmount = servicesByLeg.Sum(s => s.Price * s.Quantity);
        }
        else
        {
            _logger.LogWarning(
                "Fallback refund service calculation without leg linkage for booking {BookingId}, ticket {TicketId}",
                bookingId,
                ticket.Id);
            serviceAmount = await _dbContext.BookingServices
                .Where(bs => !bs.IsDeleted
                    && bs.BookingPassengerId == ticket.PassengerId
                    && !bs.BookingLegPassengerId.HasValue)
                .SumAsync(bs => bs.Price * bs.Quantity);
        }

        var paidUpgradeAmount = await _dbContext.TicketUpgradeRequests
            .Where(r => !r.IsDeleted
                        && r.TicketId == ticket.Id
                        && r.Status == (int)TicketUpgradeStatus.Paid)
            .SumAsync(r => (decimal?)r.PriceDifference) ?? 0m;

        return Math.Max(0, ticket.Price + serviceAmount + paidUpgradeAmount);
    }
}
