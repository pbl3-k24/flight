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
            "Booking confirmation queued to {Email}. Ref: {Reference}, Flight: {Flight}, Departure: {Departure}, Pax: {Pax}",
            userEmail, bookingReference, flightNumber, departureTime, passengerCount);
        return Task.FromResult(true);
    }

    public Task<bool> SendCancellationConfirmationAsync(string userEmail, string bookingReference, string flightNumber, decimal refundAmount)
    {
        _logger.LogInformation(
            "Cancellation confirmation queued to {Email}. Ref: {Reference}, Flight: {Flight}, Refund: {Refund}",
            userEmail, bookingReference, flightNumber, refundAmount);
        return Task.FromResult(true);
    }

    public Task<bool> SendRefundNotificationAsync(string userEmail, string bookingReference, decimal refundAmount, string refundStatus)
    {
        _logger.LogInformation(
            "Refund notification queued to {Email}. Ref: {Reference}, Refund: {Refund}, Status: {Status}",
            userEmail, bookingReference, refundAmount, refundStatus);
        return Task.FromResult(true);
    }

    public Task<bool> SendCheckInReminderAsync(string userEmail, string bookingReference, string flightNumber, DateTime departureTime)
    {
        _logger.LogInformation(
            "Check-in reminder queued to {Email}. Ref: {Reference}, Flight: {Flight}, Departure: {Departure}",
            userEmail, bookingReference, flightNumber, departureTime);
        return Task.FromResult(true);
    }
}
