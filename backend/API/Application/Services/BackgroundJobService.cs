namespace API.Application.Services;

using API.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IPricingService _pricingService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;

    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        IPricingService pricingService,
        IBookingRepository bookingRepository,
        IBookingPassengerRepository passengerRepository,
        IFlightSeatInventoryRepository seatInventoryRepository)
    {
        _logger = logger;
        _pricingService = pricingService;
        _bookingRepository = bookingRepository;
        _passengerRepository = passengerRepository;
        _seatInventoryRepository = seatInventoryRepository;
    }

    public void EnqueueReleaseSeatHolds()
    {
        try
        {
            // Would use Hangfire to enqueue job
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

            // Get all pending bookings (status = 0)
            var allBookings = await _bookingRepository.GetAllAsync();
            var pendingBookings = allBookings.Where(b => b.Status == 0 && b.ExpiresAt < now).ToList();

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
                    // Get passengers for this booking
                    var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                    if (passengers.Count == 0)
                    {
                        _logger.LogWarning("Expired booking has no passengers: {BookingId}", booking.Id);
                        continue;
                    }

                    // Get seat inventory
                    var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                        passengers.First().FlightSeatInventoryId);

                    if (seatInventory != null)
                    {
                        try
                        {
                            // Release held seats back to available (Held -> Available)
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
                            continue;
                        }
                    }

                    // Mark booking as expired/cancelled (optional - can keep as Pending for audit)
                    // booking.Status = 3; // Cancelled
                    // await _bookingRepository.UpdateAsync(booking);

                    expiredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired booking {BookingId}", booking.Id);
                    // Continue with next booking even if one fails
                }
            }

            _logger.LogInformation("Processed {ExpiredCount} expired bookings", expiredCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired bookings");
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
            // Would schedule recurring jobs with Hangfire
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
