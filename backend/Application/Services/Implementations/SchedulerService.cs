using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class SchedulerService(
    IBookingService bookingService,
    IInventoryReservationService reservationService,
    IPricingService pricingService,
    INotificationService notificationService,
    IPaymentReconciliationService reconciliationService,
    IFlightRepository flightRepository) : ISchedulerService
{
    public async Task ExpireStaleBookingsAsync()
    {
        // Fetched via BookingRepository.GetExpiredPendingAsync()
        // This is triggered by the scheduler job (e.g., Hangfire/BackgroundService)
        await reservationService.ReleaseExpiredHoldsAsync();
    }

    public Task ReleaseExpiredInventoryHoldsAsync()
        => reservationService.ReleaseExpiredHoldsAsync();

    public async Task RecalculateDynamicPricesAsync(int dayAhead = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(dayAhead);
        var flights = await flightRepository.GetAllAsync(1, 1000); // In production: filter by departure <= cutoff
        foreach (var flight in flights.Where(f => f.DepartureTime <= cutoff && f.Status == "scheduled"))
        {
            await pricingService.RecalculateFlightPricesAsync(flight.Id);
        }
    }

    public Task RetryFailedNotificationsAsync()
        => notificationService.ProcessPendingJobsAsync();

    public Task ProcessOutboxEventsAsync()
    {
        // Process outbox events for reliability (transactional outbox pattern)
        // In production: read OutboxEvent table, publish to message broker
        return Task.CompletedTask;
    }

    public async Task RunDailyReconciliationAsync()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        foreach (var gateway in new[] { "vnpay", "momo", "zalopay" })
        {
            await reconciliationService.ReconcileAsync(yesterday, gateway);
        }
    }
}
