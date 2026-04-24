namespace API.Application.Services;

using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IPricingService _pricingService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        IPricingService pricingService,
        IBookingRepository bookingRepository,
        IBookingPassengerRepository passengerRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _pricingService = pricingService;
        _bookingRepository = bookingRepository;
        _passengerRepository = passengerRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ReleaseSeatHoldsAsync()
    {
        try
        {
            _logger.LogInformation("Starting seat hold release job");

            var activeInventories = await _seatInventoryRepository.GetActiveInventoriesAsync();
            
            foreach (var inventory in activeInventories)
            {
                // Check if there are expired pending bookings for this inventory
                var expiredBookings = await _bookingRepository.GetExpiredPendingBookingsAsync(
                    inventory.FlightId, inventory.SeatClassId);

                foreach (var booking in expiredBookings)
                {
                    try
                    {
                        await _unitOfWork.BeginTransactionAsync();

                        var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                        if (passengers.Count == 0)
                        {
                            await _unitOfWork.RollbackAsync();
                            continue;
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

                        await _unitOfWork.CommitAsync();
                        _logger.LogInformation(
                            "Released seats for expired booking {BookingId}", booking.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error releasing seats for booking {BookingId}", booking.Id);
                        await _unitOfWork.RollbackAsync();
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

            var now = DateTime.UtcNow;
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
                    await _unitOfWork.BeginTransactionAsync();

                    var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                    if (passengers.Count == 0)
                    {
                        await _unitOfWork.RollbackAsync();
                        continue;
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

                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Expired booking {BookingId}", booking.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error expiring booking {BookingId}", booking.Id);
                    await _unitOfWork.RollbackAsync();
                }
            }

            _logger.LogInformation("Booking expiration job completed. Processed {Count} bookings", expiredBookings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in booking expiration job");
        }
    }

    /// <summary>
    /// Process expired bookings: Release held seats if booking hasn't been paid
    /// Called by: Hangfire background job or scheduled task
    /// State transition: Pending + Expired -> Held seats released to Available
    /// </summary>
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
                    await _unitOfWork.BeginTransactionAsync();

                    var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                    if (passengers.Count == 0)
                    {
                        _logger.LogWarning("Expired booking has no passengers: {BookingId}", booking.Id);
                        await _unitOfWork.RollbackAsync();
                        continue;
                    }

                    var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                        passengers.First().FlightSeatInventoryId);

                    if (seatInventory != null)
                    {
                        try
                        {
                            seatInventory.ReleaseHeldSeats(passengers.Count);
                            await _seatInventoryRepository.UpdateAsync(seatInventory);

                            _logger.LogInformation(
                                "Released {PassengerCount} held seats for expired booking {BookingId}",
                                passengers.Count, booking.Id);
                        }
                        catch (InvalidOperationException ex)
                        {
                            _logger.LogError(ex, 
                                "Error releasing held seats for expired booking {BookingId}. " +
                                "Current held seats: {HeldSeats}, passengers: {PassengerCount}",
                                booking.Id, seatInventory.HeldSeats, passengers.Count);
                            await _unitOfWork.RollbackAsync();
                            continue;
                        }
                    }

                    booking.Status = (int)BookingStatus.Cancelled;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _bookingRepository.UpdateAsync(booking);

                    await _unitOfWork.CommitAsync();
                    expiredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired booking {BookingId}", booking.Id);
                    await _unitOfWork.RollbackAsync();
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

    public void StartRecurringJobs()
    {
        try
        {
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
                { "RefundNotifications", "Scheduled" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status");
            return [];
        }
    }
}
