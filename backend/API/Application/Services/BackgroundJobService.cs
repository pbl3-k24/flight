namespace API.Application.Services;

using API.Application.Interfaces;
using API.Application.Common;
using API.Domain.Entities;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

public class BackgroundJobService : IBackgroundJobService
{
    private const int ChangeFlightPaymentHoldMinutes = 5;
    private const int MaxRefundRetryAttempts = 5;
    private static readonly ConcurrentQueue<VnpayRefundJob> RefundQueue = new();

    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IPricingService _pricingService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketUpgradeService _ticketUpgradeService;
    private readonly VnpayPaymentProvider _vnpayPaymentProvider;
    private readonly FlightBookingDbContext _dbContext;

    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        IPricingService pricingService,
        IBookingRepository bookingRepository,
        IBookingPassengerRepository passengerRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IUnitOfWork unitOfWork,
        IPaymentRepository paymentRepository,
        ITicketUpgradeService ticketUpgradeService,
        VnpayPaymentProvider vnpayPaymentProvider,
        FlightBookingDbContext dbContext)
    {
        _logger = logger;
        _pricingService = pricingService;
        _bookingRepository = bookingRepository;
        _passengerRepository = passengerRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _unitOfWork = unitOfWork;
        _paymentRepository = paymentRepository;
        _ticketUpgradeService = ticketUpgradeService;
        _vnpayPaymentProvider = vnpayPaymentProvider;
        _dbContext = dbContext;
    }

    public async Task ReleaseSeatHoldsAsync()
    {
        try
        {
            _logger.LogInformation("Starting seat hold release job");

            var activeInventories = await _seatInventoryRepository.GetActiveInventoriesAsync();

            foreach (var inventory in activeInventories)
            {
                var expiredBookings = await _bookingRepository.GetExpiredPendingBookingsAsync(
                    inventory.FlightId, inventory.SeatClassId);

                foreach (var booking in expiredBookings)
                {
                    try
                    {
                        await _unitOfWork.ExecuteInTransactionAsync(async () =>
                        {
                            var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                            if (passengers.Count == 0)
                            {
                                return;
                            }

                            var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                                passengers.First().FlightSeatInventoryId);

                            if (seatInventory != null)
                            {
                                var seatsToRelease = passengers.Count(p => p.PassengerType != (int)PassengerType.Infant);
                                var releasableSeats = Math.Min(seatsToRelease, seatInventory.HeldSeats);
                                if (releasableSeats > 0)
                                {
                                    seatInventory.ReleaseHeldSeats(releasableSeats);
                                    await _seatInventoryRepository.UpdateAsync(seatInventory);
                                }
                            }

                            booking.Status = (int)BookingStatus.Cancelled;
                            booking.UpdatedAt = DateTime.UtcNow;
                            await _bookingRepository.UpdateAsync(booking);

                            if (booking.PromotionId.HasValue && booking.DiscountAmount > 0)
                            {
                                await _unitOfWork.Promotions.ReleaseUsageAsync(booking.PromotionId.Value);
                            }
                        });

                        _logger.LogInformation("Released seats for expired booking {BookingId}", booking.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error releasing seats for booking {BookingId}", booking.Id);
                    }
                }
            }

            _logger.LogInformation("Seat hold release job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in seat hold release job");
        }
    }

    public async Task ExpireBookingsAsync()
    {
        try
        {
            _logger.LogInformation("Starting booking expiration job");

            var expiredBookings = await _bookingRepository.GetExpiredPendingBookingsAsync();

            if (expiredBookings.Count == 0)
            {
                _logger.LogInformation("No expired bookings to process");
                return;
            }

            foreach (var booking in expiredBookings)
            {
                try
                {
                    await _unitOfWork.ExecuteInTransactionAsync(async () =>
                    {
                        var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                        if (passengers.Count == 0)
                        {
                            return;
                        }

                        var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                            passengers.First().FlightSeatInventoryId);

                        if (seatInventory != null)
                        {
                            var seatsToRelease = passengers.Count(p => p.PassengerType != (int)PassengerType.Infant);
                            var releasableSeats = Math.Min(seatsToRelease, seatInventory.HeldSeats);
                            if (releasableSeats > 0)
                            {
                                seatInventory.ReleaseHeldSeats(releasableSeats);
                                await _seatInventoryRepository.UpdateAsync(seatInventory);
                            }
                        }

                        booking.Status = (int)BookingStatus.Cancelled;
                        booking.UpdatedAt = DateTime.UtcNow;
                        await _bookingRepository.UpdateAsync(booking);

                        if (booking.PromotionId.HasValue && booking.DiscountAmount > 0)
                        {
                            await _unitOfWork.Promotions.ReleaseUsageAsync(booking.PromotionId.Value);
                        }
                    });

                    _logger.LogInformation("Expired booking {BookingId}", booking.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error expiring booking {BookingId}", booking.Id);
                }
            }

            _logger.LogInformation("Booking expiration job completed. Processed {Count} bookings", expiredBookings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in booking expiration job");
        }
    }

    public async Task ProcessExpiredBookingsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var nowVn = VietnamTime.UtcNowInVietnam();
            _logger.LogInformation("Processing expired bookings at {TimeVn} (VN, UTC+7)", nowVn);

            await ProcessDepartedTicketsAsync(now);
            await ProcessExpiredChangeFlightAwaitingPaymentsAsync(now);

            var expiredUpgradeRequests = await _ticketUpgradeService.ExpirePendingRequestsAsync();
            if (expiredUpgradeRequests > 0)
            {
                _logger.LogInformation("Expired {Count} pending ticket upgrade requests", expiredUpgradeRequests);
            }

            var allBookings = await _bookingRepository.GetAllAsync();
            var pendingBookings = allBookings
                .Where(b => b.Status == (int)BookingStatus.Pending && b.ExpiresAt < now)
                .ToList();

            if (pendingBookings.Count == 0)
            {
                _logger.LogInformation("No expired bookings to process");
                return;
            }

            int expiredCount = 0;
            foreach (var booking in pendingBookings)
            {
                try
                {
                    await _unitOfWork.ExecuteInTransactionAsync(async () =>
                    {
                        var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                        if (passengers.Count == 0)
                        {
                            _logger.LogWarning("Expired booking has no passengers: {BookingId}", booking.Id);
                            return;
                        }

                        var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                            passengers.First().FlightSeatInventoryId);

                        if (seatInventory != null)
                        {
                            var seatsToRelease = passengers.Count(p => p.PassengerType != (int)PassengerType.Infant);
                            var releasableSeats = Math.Min(seatsToRelease, seatInventory.HeldSeats);
                            if (releasableSeats > 0)
                            {
                                seatInventory.ReleaseHeldSeats(releasableSeats);
                                await _seatInventoryRepository.UpdateAsync(seatInventory);
                            }
                        }

                        booking.Status = (int)BookingStatus.Cancelled;
                        booking.UpdatedAt = DateTime.UtcNow;
                        await _bookingRepository.UpdateAsync(booking);

                        if (booking.PromotionId.HasValue && booking.DiscountAmount > 0)
                        {
                            await _unitOfWork.Promotions.ReleaseUsageAsync(booking.PromotionId.Value);
                        }
                    });

                    expiredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired booking {BookingId}", booking.Id);
                }
            }

            _logger.LogInformation("Processed {ExpiredCount} expired bookings", expiredCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired bookings");
        }
    }

    private async Task ProcessDepartedTicketsAsync(DateTime nowUtc)
    {
        var issuedDepartedTickets = await _dbContext.Tickets
            .Where(t => !t.IsDeleted && t.Status == 0)
            .Join(
                _dbContext.Flights.Where(f => !f.IsDeleted),
                ticket => ticket.FlightId,
                flight => flight.Id,
                (ticket, flight) => new { Ticket = ticket, flight.DepartureTime })
            .Where(x => x.DepartureTime <= nowUtc)
            .Select(x => x.Ticket)
            .ToListAsync();

        if (issuedDepartedTickets.Count == 0)
        {
            return;
        }

        foreach (var ticket in issuedDepartedTickets)
        {
            ticket.Status = 1; // Used
            ticket.UpdatedAt = nowUtc;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Auto-marked {Count} departed issued tickets as used", issuedDepartedTickets.Count);
    }

    private async Task ProcessExpiredChangeFlightAwaitingPaymentsAsync(DateTime nowUtc)
    {
        var cutoff = nowUtc.AddMinutes(-ChangeFlightPaymentHoldMinutes);
        var expiredChangeRequests = await _dbContext.BookingChangeRequests
            .Where(r => !r.IsDeleted && r.Status == 1 && r.CreatedAt < cutoff)
            .ToListAsync();

        if (expiredChangeRequests.Count == 0)
        {
            return;
        }

        foreach (var request in expiredChangeRequests)
        {
            try
            {
                var leg = await _dbContext.BookingLegs
                    .FirstOrDefaultAsync(l => !l.IsDeleted
                        && l.BookingId == request.BookingId
                        && l.LegType == request.LegType);
                if (leg == null)
                {
                    request.Status = 3;
                    request.UpdatedAt = nowUtc;
                    continue;
                }

                var issuedTickets = await _dbContext.Tickets
                    .Where(t => !t.IsDeleted
                        && t.BookingId == request.BookingId
                        && t.FlightId == request.OldFlightId
                        && t.Status == 0)
                    .ToListAsync();
                var passengerIds = issuedTickets.Select(t => t.BookingPassengerId).Distinct().ToList();
                var passengerTypeById = await _dbContext.BookingPassengers
                    .Where(p => passengerIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.PassengerType);
                var seatCount = issuedTickets.Count(t =>
                    passengerTypeById.TryGetValue(t.BookingPassengerId, out var passengerType)
                    && passengerType != (int)PassengerType.Infant);

                if (seatCount > 0)
                {
                    var newInventory = await _seatInventoryRepository
                        .GetByFlightAndSeatClassAsync(request.NewFlightId, leg.SeatClassId);
                    if (newInventory != null)
                    {
                        await _seatInventoryRepository.TryReleaseHeldSeatsAtomicAsync(newInventory.Id, seatCount);
                    }
                }

                request.Status = 3;
                request.UpdatedAt = nowUtc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring change-flight awaiting payment request {RequestId}", request.Id);
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation(
            "Expired {Count} change-flight awaiting payment requests older than {Minutes} minutes",
            expiredChangeRequests.Count,
            ChangeFlightPaymentHoldMinutes);
    }

    public async Task ProcessFlightDisruptionTimeoutsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _dbContext.FlightDisruptionDecisions
            .Where(d => !d.IsDeleted && d.Status == 0 && d.DecisionDeadline < now)
            .ToListAsync();

        foreach (var decision in expired)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(decision.BookingId);
                if (booking == null || booking.IsDeleted)
                {
                    decision.Status = 2;
                    decision.DecisionType = 3;
                    decision.DecidedAt = now;
                    decision.UpdatedAt = now;
                    continue;
                }

                var tickets = await _dbContext.Tickets
                    .Where(t => !t.IsDeleted && t.BookingId == booking.Id)
                    .ToListAsync();

                foreach (var ticket in tickets.Where(t => t.Status == 0))
                {
                    ticket.Status = 4;
                    ticket.UpdatedAt = now;
                }

                var payment = (await _paymentRepository.GetByBookingIdAsync(booking.Id))
                    .Where(p => !p.IsDeleted && p.Status == (int)PaymentStatus.Completed)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();
                if (payment != null)
                {
                    EnqueueVnpayRefund(booking.Id, "Auto refund after disruption decision timeout");
                }

                booking.Status = (int)BookingStatus.Cancelled;
                booking.UpdatedAt = now;
                await _bookingRepository.UpdateAsync(booking);

                decision.Status = 2;
                decision.DecisionType = 3;
                decision.DecidedAt = now;
                decision.UpdatedAt = now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing disruption timeout {DecisionId}", decision.Id);
            }
        }

        if (expired.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    public void EnqueueReleaseSeatHolds()
    {
        try
        {
            _logger.LogInformation("Seat hold release job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing seat hold release job");
        }
    }

    public void EnqueueExpireBookings()
    {
        try
        {
            _logger.LogInformation("Booking expiration job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing booking expiration job");
        }
    }

    public void EnqueueUpdatePrices()
    {
        try
        {
            _logger.LogInformation("Price update job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing price update job");
        }
    }

    public void EnqueueBookingReminders()
    {
        try
        {
            _logger.LogInformation("Booking reminder job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing booking reminder job");
        }
    }

    public void EnqueueRefundNotifications()
    {
        try
        {
            _logger.LogInformation("Refund notification job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing refund notification job");
        }
    }

    public void EnqueueGenerateReports()
    {
        try
        {
            _logger.LogInformation("Report generation job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing report generation job");
        }
    }

    public void EnqueueVnpayRefund(int bookingId, string reason)
    {
        try
        {
            var job = new VnpayRefundJob
            {
                BookingId = bookingId,
                Reason = string.IsNullOrWhiteSpace(reason) ? $"Refund booking #{bookingId}" : reason.Trim(),
                EnqueuedAt = DateTime.UtcNow,
                RetryCount = 0,
                NextAttemptAt = DateTime.UtcNow
            };

            RefundQueue.Enqueue(job);
            _logger.LogInformation("Enqueued VNPAY refund job for booking {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing VNPAY refund job for booking {BookingId}", bookingId);
        }
    }

    public async Task ProcessVnpayRefundQueueAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested && RefundQueue.TryDequeue(out var job))
        {
            if (job.NextAttemptAt > DateTime.UtcNow)
            {
                RefundQueue.Enqueue(job);
                continue;
            }

            var success = await ProcessSingleVnpayRefundAsync(job, cancellationToken);
            if (success)
            {
                continue;
            }

            if (job.RetryCount >= MaxRefundRetryAttempts)
            {
                await MarkRefundFailedAsync(job.BookingId);
                _logger.LogError(
                    "VNPAY refund permanently failed after {RetryCount} retries for booking {BookingId}",
                    job.RetryCount,
                    job.BookingId);
                continue;
            }

            job.RetryCount++;
            job.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, job.RetryCount));
            RefundQueue.Enqueue(job);

            _logger.LogWarning(
                "VNPAY refund failed for booking {BookingId}. Retry {RetryCount}/{MaxRetries} at {NextAttempt}",
                job.BookingId,
                job.RetryCount,
                MaxRefundRetryAttempts,
                job.NextAttemptAt);
        }
    }

    public void StartRecurringJobs()
    {
        try
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await ProcessVnpayRefundQueueAsync();
                    await ProcessFlightDisruptionTimeoutsAsync();
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            });

            _logger.LogInformation("Recurring jobs started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting recurring jobs");
        }
    }

    public async Task<Dictionary<string, string>> GetJobStatusAsync()
    {
        try
        {
            return new Dictionary<string, string>
            {
                { "ReleaseSeatHolds", "Scheduled" },
                { "ExpireBookings", "Scheduled" },
                { "UpdatePrices", "Scheduled" },
                { "BookingReminders", "Scheduled" },
                { "RefundNotifications", "Scheduled" },
                { "GenerateReports", "Scheduled" },
                { "VnpayRefundQueueSize", RefundQueue.Count.ToString() }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status");
            return [];
        }
    }

    private async Task<bool> ProcessSingleVnpayRefundAsync(VnpayRefundJob job, CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(job.BookingId);
            if (booking == null)
            {
                _logger.LogWarning("Cannot process VNPAY refund. Booking not found: {BookingId}", job.BookingId);
                return true;
            }

            var payment = (await _paymentRepository.GetByBookingIdAsync(job.BookingId))
                .FirstOrDefault(p => p.Status == 1 && string.Equals(p.Provider, "VNPAY", StringComparison.OrdinalIgnoreCase));

            if (payment == null || string.IsNullOrWhiteSpace(payment.TransactionRef))
            {
                _logger.LogWarning("No completed VNPAY payment found for booking {BookingId}", job.BookingId);
                return true;
            }

            var request = new VnpayRefundRequest
            {
                TxnRef = payment.TransactionRef,
                TransactionDate = (payment.PaidAt ?? payment.CreatedAt).ToString("yyyyMMddHHmmss"),
                Amount = payment.Amount,
                OrderInfo = job.Reason,
                CreateBy = "system-worker"
            };

            var response = await _vnpayPaymentProvider.ProcessRefundAsync(request, cancellationToken);
            if (!response.Success)
            {
                _logger.LogWarning(
                    "VNPAY refund API failed for booking {BookingId}. Code: {Code}, Message: {Message}",
                    job.BookingId,
                    response.ResponseCode,
                    response.Message);
                return false;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                payment.Status = 3;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                booking.Status = (int)BookingStatus.Refunded;
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(booking);
            });

            _logger.LogInformation("VNPAY refund success for booking {BookingId}. TxnRef: {TxnRef}", job.BookingId, request.TxnRef);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing VNPAY refund for booking {BookingId}", job.BookingId);
            return false;
        }
    }

    private async Task MarkRefundFailedAsync(int bookingId)
    {
        try
        {
            var payment = (await _paymentRepository.GetByBookingIdAsync(bookingId))
                .FirstOrDefault(p => p.Status == 1 && string.Equals(p.Provider, "VNPAY", StringComparison.OrdinalIgnoreCase));

            if (payment == null)
            {
                return;
            }

            payment.Status = (int)PaymentStatus.RefundFailed;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking refund failed for booking {BookingId}", bookingId);
        }
    }

    private sealed class VnpayRefundJob
    {
        public int BookingId { get; set; }
        public string Reason { get; set; } = null!;
        public int RetryCount { get; set; }
        public DateTime EnqueuedAt { get; set; }
        public DateTime NextAttemptAt { get; set; }
    }
}
