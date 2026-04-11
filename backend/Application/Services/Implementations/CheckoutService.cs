using FlightBooking.Application.DTOs.Booking;
using FlightBooking.Application.DTOs.Payment;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class CheckoutService(
    IBookingService bookingService,
    IPaymentService paymentService,
    IInventoryReservationService reservationService,
    ITicketingService ticketingService,
    INotificationService notificationService,
    IBookingRepository bookingRepository) : ICheckoutService
{
    public async Task<CheckoutSessionDto> InitiateCheckoutAsync(CheckoutRequest request, Guid userId)
    {
        // 1. Create booking (validates, holds inventory, snapshots prices)
        var createRequest = new CreateBookingRequest(request.Items, request.PromotionCode);
        var booking = await bookingService.CreateAsync(createRequest, userId);

        // 2. Create payment intent with gateway
        var paymentInitiation = await paymentService.InitiatePaymentAsync(
            booking.Id, request.Gateway, request.ReturnUrl);

        return new CheckoutSessionDto(
            booking.Id,
            booking.BookingCode,
            booking.TotalAmount,
            paymentInitiation.PaymentUrl,
            booking.ExpiresAt ?? DateTime.UtcNow.AddMinutes(15));
    }

    public async Task CompleteCheckoutAsync(Guid bookingId, string paymentTransactionRef)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status != "pending_payment")
            return; // idempotent - already processed

        // 1. Confirm inventory (convert held → sold)
        foreach (var group in booking.Items.GroupBy(i => (i.FlightId, i.FareClassId)))
        {
            await reservationService.ConfirmSeatsAsync(
                group.Key.FlightId, group.Key.FareClassId, group.Count(), bookingId);
        }

        // 2. Confirm booking
        booking.Status = "confirmed";
        booking.UpdatedAt = DateTime.UtcNow;
        await bookingRepository.SaveChangesAsync();

        // 3. Issue tickets
        await ticketingService.IssueTicketsAsync(bookingId);

        // 4. Send confirmation email
        await notificationService.SendBookingConfirmedAsync(bookingId);
    }

    public async Task AbortCheckoutAsync(Guid bookingId, string reason)
    {
        await bookingService.CancelAsync(bookingId, reason, Guid.Empty);
    }
}
