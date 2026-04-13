namespace API.Application.Services;

using API.Application.Interfaces;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendBookingConfirmationAsync(string userEmail, string bookingReference, string flightNumber, DateTime departureTime, int passengerCount)
    {
        _logger.LogInformation(
            "Booking confirmation queued. Ref: {Reference}, Flight: {Flight}, Departure: {Departure}, Pax: {Pax}",
            bookingReference, flightNumber, departureTime, passengerCount);
        return Task.FromResult(true);
    }

    public Task<bool> SendCancellationConfirmationAsync(string userEmail, string bookingReference, string flightNumber, decimal refundAmount)
    {
        _logger.LogInformation(
            "Cancellation confirmation queued. Ref: {Reference}, Flight: {Flight}, Refund: {Refund}",
            bookingReference, flightNumber, refundAmount);
        return Task.FromResult(true);
    }

    public Task<bool> SendRefundNotificationAsync(string userEmail, string bookingReference, decimal refundAmount, string refundStatus)
    {
        _logger.LogInformation(
            "Refund notification queued. Ref: {Reference}, Refund: {Refund}, Status: {Status}",
            bookingReference, refundAmount, refundStatus);
        return Task.FromResult(true);
    }

    public Task<bool> SendCheckInReminderAsync(string userEmail, string bookingReference, string flightNumber, DateTime departureTime)
    {
        _logger.LogInformation(
            "Check-in reminder queued. Ref: {Reference}, Flight: {Flight}, Departure: {Departure}",
            bookingReference, flightNumber, departureTime);
        return Task.FromResult(true);
    }
}
