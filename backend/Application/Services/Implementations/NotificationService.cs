using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace FlightBooking.Application.Services.Implementations;

public class NotificationService(
    INotificationJobRepository jobRepository,
    IEmailTemplateRepository templateRepository,
    IEmailService emailService,
    IBookingRepository bookingRepository,
    IRefundRepository refundRepository,
    ITicketRepository ticketRepository) : INotificationService
{
    public async Task SendBookingConfirmedAsync(Guid bookingId)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(bookingId);
        if (booking is null) return;

        await EnqueueEmailAsync(
            booking.User.Email,
            "booking_confirmed",
            new { BookingCode = booking.BookingCode, TotalAmount = booking.TotalAmount });
    }

    public async Task SendFlightChangedAsync(Guid flightId, string changeType)
    {
        var affectedBookings = await bookingRepository.GetConfirmedByFlightAsync(flightId);
        foreach (var booking in affectedBookings)
        {
            await EnqueueEmailAsync(
                booking.User.Email,
                "flight_changed",
                new { BookingCode = booking.BookingCode, ChangeType = changeType, FlightId = flightId });
        }
    }

    public async Task SendRefundProcessedAsync(Guid refundId)
    {
        var refund = await refundRepository.GetByIdWithPaymentAsync(refundId);
        if (refund?.Payment?.Booking?.User is null) return;

        await EnqueueEmailAsync(
            refund.Payment.Booking.User.Email,
            "refund_processed",
            new { RefundId = refundId, Amount = refund.Amount, Status = refund.Status });
    }

    public async Task SendTicketIssuedAsync(Guid ticketId)
    {
        var ticket = await ticketRepository.GetByIdWithDetailsAsync(ticketId);
        if (ticket?.BookingItem?.Booking?.User is null) return;

        await EnqueueEmailAsync(
            ticket.BookingItem.Booking.User.Email,
            "ticket_issued",
            new { TicketNumber = ticket.TicketNumber, FlightNumber = ticket.BookingItem.Flight?.FlightNumber });
    }

    public async Task SendOtpAsync(Guid userId, string purpose)
    {
        // Delegated to VerificationService which calls EmailService directly
        throw new NotSupportedException("Use IVerificationService.SendEmailOtpAsync for OTP sending.");
    }

    public async Task ProcessPendingJobsAsync()
    {
        var jobs = await jobRepository.GetPendingAsync(batchSize: 50);
        foreach (var job in jobs)
        {
            try
            {
                await emailService.SendAsync(job.Recipient, job.TemplateKey, JsonSerializer.Deserialize<object>(job.Payload)!);
                job.Status = "sent";
                job.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                job.RetryCount++;
                job.Status = job.RetryCount >= job.MaxRetries ? "failed" : "pending";
                job.ErrorMessage = ex.Message;
            }
        }

        await jobRepository.SaveChangesAsync();
    }

    private async Task EnqueueEmailAsync(string recipient, string templateKey, object payload)
    {
        var job = new NotificationJob
        {
            Id = Guid.NewGuid(),
            Type = "email",
            Recipient = recipient,
            TemplateKey = templateKey,
            Payload = JsonSerializer.Serialize(payload),
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };
        await jobRepository.AddAsync(job);
        await jobRepository.SaveChangesAsync();
    }
}
