namespace API.Infrastructure.Services;

using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlContent)
    {
        try
        {
            if (!IsValidEmail(email))
            {
                _logger.LogWarning("Invalid email format: {Email}", email);
                throw new ArgumentException("Invalid email format", nameof(email));
            }

            var smtpHost = _config["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host is not configured");
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUsername = _config["Smtp:Username"];
            var smtpPassword = _config["Smtp:Password"];
            var fromEmail = _config["Smtp:FromEmail"] ?? smtpUsername;

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                Timeout = 10000
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = htmlContent,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw;
        }
    }

    public async Task SendVerificationEmailAsync(string email, string verificationCode)
    {
        var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:7001";
        var verificationLink = $"{baseUrl}/auth/verify-email?code={verificationCode}";

        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Email Verification</h2>
                    <p>Thank you for signing up! Please verify your email address by clicking the link below:</p>
                    <a href='{verificationLink}' class='button'>Verify Email</a>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't create this account, please ignore this email.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, "Email Verification", htmlContent);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetCode)
    {
        var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:7001";
        var resetLink = $"{baseUrl}/auth/reset-password?code={resetCode}";

        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your password. Click the link below to proceed:</p>
                    <a href='{resetLink}' class='button'>Reset Password</a>
                    <p>This link will expire in 1 hour.</p>
                    <p>If you didn't request a password reset, please ignore this email.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, "Password Reset", htmlContent);
    }

    public async Task SendBookingConfirmationAsync(string email, Booking booking)
    {
        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                    .booking-info {{ margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }}
                    .info-row {{ display: flex; justify-content: space-between; margin: 10px 0; }}
                    .label {{ font-weight: bold; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Booking Confirmation</h1>
                    </div>
                    <div class='booking-info'>
                        <div class='info-row'>
                            <span class='label'>Booking Code:</span>
                            <span>{booking.BookingCode}</span>
                        </div>
                        <div class='info-row'>
                            <span class='label'>Total Price:</span>
                            <span>${booking.FinalAmount:F2}</span>
                        </div>
                        <div class='info-row'>
                            <span class='label'>Status:</span>
                            <span>{booking.Status}</span>
                        </div>
                    </div>
                    <p>Your booking has been confirmed. You can view your tickets and manage your booking in your account.</p>
                    <p>Thank you for booking with us!</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, $"Booking Confirmation - {booking.BookingCode}", htmlContent);
    }

    public async Task SendTicketEmailAsync(string email, Booking booking, List<Ticket> tickets)
    {
        var ticketsList = string.Join("\n", tickets.Select(t => $"<li>Ticket #{t.TicketNumber}</li>"));

        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                    .tickets-list {{ margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Your Tickets</h1>
                    </div>
                    <div class='tickets-list'>
                        <p>Your tickets are ready! Here are your ticket numbers:</p>
                        <ul>
                            {ticketsList}
                        </ul>
                        <p>Please keep these numbers safe. You'll need them for check-in.</p>
                    </div>
                    <p>Have a great flight!</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, $"Your Tickets - {booking.BookingCode}", htmlContent);
    }

    public async Task SendBookingCancellationAsync(string email, Booking booking)
    {
        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                    .booking-info {{ margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Booking Cancelled</h1>
                    </div>
                    <div class='booking-info'>
                        <p>Your booking has been cancelled.</p>
                        <p><strong>Booking Code:</strong> {booking.BookingCode}</p>
                        <p>If you requested a refund, it will be processed according to our refund policy.</p>
                        <p>If you have any questions, please contact our support team.</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, $"Booking Cancelled - {booking.BookingCode}", htmlContent);
    }

    public async Task SendNotificationAsync(string email, string title, string content)
    {
        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; }}
                    .content {{ margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>{title}</h1>
                    </div>
                    <div class='content'>
                        {content}
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, title, htmlContent);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
