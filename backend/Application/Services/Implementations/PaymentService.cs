using FlightBooking.Application.DTOs.Payment;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class PaymentService(
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository) : IPaymentService
{
    public async Task<PaymentDto> GetByIdAsync(Guid id)
    {
        var payment = await paymentRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found.");
        return MapToDto(payment);
    }

    public async Task<IEnumerable<PaymentDto>> GetByBookingAsync(Guid bookingId)
    {
        var payments = await paymentRepository.GetByBookingAsync(bookingId);
        return payments.Select(MapToDto);
    }

    public async Task<PaymentInitiationDto> InitiatePaymentAsync(Guid bookingId, string gateway, string returnUrl)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status != "pending_payment")
            throw new InvalidOperationException($"Booking is in status '{booking.Status}', cannot initiate payment.");

        var transactionRef = $"{gateway.ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N[..8]}";

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            Gateway = gateway,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            Status = "pending",
            TransactionRef = transactionRef,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await paymentRepository.AddAsync(payment);
        await paymentRepository.SaveChangesAsync();

        // In production: call gateway API to get actual payment URL
        // For now, return a simulated URL
        var paymentUrl = $"{returnUrl}?gateway={gateway}&ref={transactionRef}&amount={booking.TotalAmount}";

        return new PaymentInitiationDto(
            payment.Id,
            paymentUrl,
            transactionRef,
            DateTime.UtcNow.AddMinutes(15));
    }

    public async Task<PaymentDto> QueryPaymentStatusAsync(Guid paymentId)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");
        // In production: query gateway for status update
        return MapToDto(payment);
    }

    private static PaymentDto MapToDto(Payment p) =>
        new(p.Id, p.BookingId, p.Gateway, p.Amount, p.Currency, p.Status,
            p.TransactionRef, p.GatewayTransactionId, p.CreatedAt);
}

public class PaymentWebhookService(
    IPaymentRepository paymentRepository,
    IIdempotencyService idempotencyService,
    ICheckoutService checkoutService) : IPaymentWebhookService
{
    public async Task HandleWebhookAsync(string gateway, string rawPayload, IReadOnlyDictionary<string, string> headers)
    {
        // Extract idempotency key from headers or payload
        var idempotencyKey = headers.TryGetValue("x-idempotency-key", out var key) ? key : rawPayload.GetHashCode().ToString();

        if (await idempotencyService.IsAlreadyProcessedAsync(idempotencyKey, "webhook"))
            return; // Duplicate webhook, ignore

        // In production: validate webhook signature for each gateway
        // VNPay: SecureHash validation
        // MoMo: signature HMAC validation
        // ZaloPay: MAC validation

        // For now, just log that webhook was received
        // Parse payload to extract payment status and booking ref
        // Then call CheckoutService.CompleteCheckoutAsync or AbortCheckoutAsync

        await idempotencyService.SetResponseAsync(idempotencyKey, "webhook", "processed", TimeSpan.FromDays(7));
    }
}

public class PaymentReconciliationService(IPaymentRepository paymentRepository) : IPaymentReconciliationService
{
    public Task<ReconciliationReportDto> ReconcileAsync(DateOnly date, string gateway)
    {
        // In production: fetch transactions from gateway statement API
        // Compare with local payment records
        // Flag discrepancies
        var report = new ReconciliationReportDto(date, gateway, 0, 0, 0, 0);
        return Task.FromResult(report);
    }

    public Task<IEnumerable<ReconciliationDiscrepancyDto>> GetDiscrepanciesAsync(DateOnly date, string? gateway = null)
        => Task.FromResult(Enumerable.Empty<ReconciliationDiscrepancyDto>());
}
