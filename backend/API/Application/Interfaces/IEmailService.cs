namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IEmailService
{
    /// <summary>
    /// Sends a generic email with HTML content.
    /// </summary>
    Task SendEmailAsync(string email, string subject, string htmlContent);

    /// <summary>
    /// Sends an email verification message.
    /// </summary>
    Task SendVerificationEmailAsync(string email, string verificationCode);

    /// <summary>
    /// Sends a password reset email.
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string resetCode);

    /// <summary>
    /// Sends a booking confirmation email.
    /// </summary>
    Task SendBookingConfirmationAsync(string email, Booking booking);

    /// <summary>
    /// Sends tickets via email.
    /// </summary>
    Task SendTicketEmailAsync(string email, Booking booking, List<Ticket> tickets);

    /// <summary>
    /// Sends a booking cancellation notification.
    /// </summary>
    Task SendBookingCancellationAsync(string email, Booking booking);

    /// <summary>
    /// Sends a generic notification email.
    /// </summary>
    Task SendNotificationAsync(string email, string title, string content);
}
