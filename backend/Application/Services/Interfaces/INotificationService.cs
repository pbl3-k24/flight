namespace FlightBooking.Application.Services.Interfaces;

public interface INotificationService
{
    Task SendBookingConfirmedAsync(Guid bookingId);
    Task SendFlightChangedAsync(Guid flightId, string changeType);
    Task SendRefundProcessedAsync(Guid refundId);
    Task SendTicketIssuedAsync(Guid ticketId);
    Task SendOtpAsync(Guid userId, string purpose);
    Task ProcessPendingJobsAsync();
}
