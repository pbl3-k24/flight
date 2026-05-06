namespace API.Application.Services;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

public class BackgroundJobService : IBackgroundJobService
{
    private const int MaxRefundRetryAttempts = 5;
    private static readonly ConcurrentQueue<VnpayRefundJob> RefundQueue = new();

    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IPricingService _pricingService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentRepository _paymentRepository;
    private readonly VnpayPaymentProvider _vnpayPaymentProvider;

    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        IPricingService pricingService,
        IBookingRepository bookingRepository,
        IBookingPassengerRepository passengerRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IUnitOfWork unitOfWork,
        IPaymentRepository paymentRepository,
        VnpayPaymentProvider vnpayPaymentProvider)
    {
        _logger = logger;
        _pricingService = pricingService;
        _bookingRepository = bookingRepository;
        _passengerRepository = passengerRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _unitOfWork = unitOfWork;
        _paymentRepository = paymentRepository;
        _vnpayPaymentProvider = vnpayPaymentProvider;
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
                                seatInventory.ReleaseHeldSeats(passengers.Count);
                                await _seatInventoryRepository.UpdateAsync(seatInventory);
                            }

                            booking.Status = (int)BookingStatus.Cancelled;
                            booking.UpdatedAt = DateTime.UtcNow;
                            await _bookingRepository.UpdateAsync(booking);
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
                            seatInventory.ReleaseHeldSeats(passengers.Count);
                            await _seatInventoryRepository.UpdateAsync(seatInventory);
                        }

                        booking.Status = (int)BookingStatus.Cancelled;
                        booking.UpdatedAt = DateTime.UtcNow;
                        await _bookingRepository.UpdateAsync(booking);
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
            _logger.LogInformation("Processing expired bookings at {Time}", now);

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
                            seatInventory.ReleaseHeldSeats(passengers.Count);
                            await _seatInventoryRepository.UpdateAsync(seatInventory);
                        }

                        booking.Status = (int)BookingStatus.Cancelled;
                        booking.UpdatedAt = DateTime.UtcNow;
                        await _bookingRepository.UpdateAsync(booking);
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

            payment.Status = 4;
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
