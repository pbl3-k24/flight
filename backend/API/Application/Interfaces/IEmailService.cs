namespace API.Application.Interfaces;

/// <summary>
/// Service interface for email notifications.
/// Handles sending emails to users and passengers.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a booking confirmation email to the user.
    /// </summary>
    /// <param name="userEmail">Recipient email address.</param>
    /// <param name="bookingReference">Booking reference code.</param>
    /// <param name="flightNumber">Flight number.</param>
    /// <param name="departureTime">Flight departure time.</param>
    /// <param name="passengerCount">Number of passengers.</param>
    /// <returns>True if email was sent successfully, false otherwise.</returns>
    Task<bool> SendBookingConfirmationAsync(
        string userEmail,
        string bookingReference,
        string flightNumber,
        DateTime departureTime,
        int passengerCount);

    /// <summary>
    /// Sends a booking cancellation email to the user.
    /// </summary>
    /// <param name="userEmail">Recipient email address.</param>
    /// <param name="bookingReference">Booking reference code.</param>
    /// <param name="flightNumber">Flight number.</param>
    /// <param name="refundAmount">Amount being refunded.</param>
    /// <returns>True if email was sent successfully, false otherwise.</returns>
    Task<bool> SendCancellationConfirmationAsync(
        string userEmail,
        string bookingReference,
        string flightNumber,
        decimal refundAmount);

    /// <summary>
    /// Sends a refund notification email to the user.
    /// </summary>
    /// <param name="userEmail">Recipient email address.</param>
    /// <param name="bookingReference">Booking reference code.</param>
    /// <param name="refundAmount">Amount refunded.</param>
    /// <param name="refundStatus">Status of refund (e.g., "Processed", "Pending").</param>
    /// <returns>True if email was sent successfully, false otherwise.</returns>
    Task<bool> SendRefundNotificationAsync(
        string userEmail,
        string bookingReference,
        decimal refundAmount,
        string refundStatus);

    /// <summary>
    /// Sends a check-in reminder email to the user.
    /// </summary>
    /// <param name="userEmail">Recipient email address.</param>
    /// <param name="bookingReference">Booking reference code.</param>
    /// <param name="flightNumber">Flight number.</param>
    /// <param name="departureTime">Flight departure time.</param>
    /// <returns>True if email was sent successfully, false otherwise.</returns>
    Task<bool> SendCheckInReminderAsync(
        string userEmail,
        string bookingReference,
        string flightNumber,
        DateTime departureTime);
}
