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
    /// Starts recurring jobs.
    /// </summary>
    void StartRecurringJobs();

    /// <summary>
    /// Gets job status.
    /// </summary>
    Task<Dictionary<string, string>> GetJobStatusAsync();
}
