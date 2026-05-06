namespace API.Application.Interfaces;

public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a job to release expired seat holds.
    /// </summary>
    void EnqueueReleaseSeatHolds();

    /// <summary>
    /// Enqueues a job to expire pending bookings.
    /// </summary>
    void EnqueueExpireBookings();

    /// <summary>
    /// Enqueues a job to update dynamic prices.
    /// </summary>
    void EnqueueUpdatePrices();

    /// <summary>
    /// Enqueues a job to send booking reminders.
    /// </summary>
    void EnqueueBookingReminders();

    /// <summary>
    /// Enqueues a job to send refund notifications.
    /// </summary>
    void EnqueueRefundNotifications();

    /// <summary>
    /// Enqueues a job to generate reports.
    /// </summary>
    void EnqueueGenerateReports();

    /// <summary>
    /// Enqueues a VNPAY refund job to be processed asynchronously.
    /// </summary>
    /// <param name="bookingId">Booking ID to refund</param>
    /// <param name="reason">Refund reason/order info</param>
    void EnqueueVnpayRefund(int bookingId, string reason);

    /// <summary>
    /// Processes pending VNPAY refund jobs from queue.
    /// </summary>
    Task ProcessVnpayRefundQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes expired bookings.
    /// </summary>
    Task ProcessExpiredBookingsAsync();

    /// <summary>
    /// Starts recurring jobs.
    /// </summary>
    void StartRecurringJobs();

    /// <summary>
    /// Gets job status.
    /// </summary>
    Task<Dictionary<string, string>> GetJobStatusAsync();
}
