namespace FlightBooking.Application.Services.Interfaces;

public interface ISchedulerService
{
    /// <summary>Expire bookings that are past their hold timeout.</summary>
    Task ExpireStaleBookingsAsync();

    /// <summary>Release inventory holds that have exceeded hold duration.</summary>
    Task ReleaseExpiredInventoryHoldsAsync();

    /// <summary>Recalculate dynamic prices for flights departing in the next N days.</summary>
    Task RecalculateDynamicPricesAsync(int dayAhead = 30);

    /// <summary>Retry failed notification jobs.</summary>
    Task RetryFailedNotificationsAsync();

    /// <summary>Process outbox events (transactional outbox pattern).</summary>
    Task ProcessOutboxEventsAsync();

    /// <summary>Daily payment reconciliation.</summary>
    Task RunDailyReconciliationAsync();
}
