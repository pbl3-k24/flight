namespace API.Application.Services;

using API.Application.Dtos.Payment;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentService> _logger;
    private readonly IPaymentProviderFactory _paymentProviderFactory;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IEmailService emailService,
        ILogger<PaymentService> logger,
        IPaymentProviderFactory paymentProviderFactory)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _emailService = emailService;
        _logger = logger;
        _paymentProviderFactory = paymentProviderFactory;
    }

    public async Task<PaymentResponse> InitiatePaymentAsync(int bookingId, InitiatePaymentDto dto)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            var payment = new Payment
            {
                BookingId = bookingId,
                Provider = dto.PaymentMethod,
                Method = dto.PaymentMethod,
                Amount = booking.FinalAmount,
                Status = 0, // Pending
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);

            var provider = _paymentProviderFactory.CreateProvider(dto.PaymentMethod);
            var providerRequest = new PaymentProviderRequest
            {
                Amount = booking.FinalAmount,
                BookingId = bookingId,
                Email = booking.ContactEmail
            };

            var providerResponse = await provider.GeneratePaymentLinkAsync(providerRequest);
            createdPayment.TransactionRef = providerResponse.TransactionId;
            createdPayment.QrCodeData = providerResponse.QrCode;
            await _paymentRepository.UpdateAsync(createdPayment);

            _logger.LogInformation("Payment initiated: {PaymentId} for booking {BookingId}", createdPayment.Id, bookingId);

            return new PaymentResponse
            {
                PaymentId = createdPayment.Id,
                BookingId = bookingId,
                Status = "Pending",
                Amount = booking.FinalAmount,
                Provider = dto.PaymentMethod,
                TransactionRef = providerResponse.TransactionId,
                PaymentLink = providerResponse.PaymentLink,
                QrCode = providerResponse.QrCode,
                CreatedAt = createdPayment.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            throw;
        }
    }

    public async Task<bool> ProcessPaymentAsync(int paymentId, PaymentCallbackDto callback)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return false;
            }

            if (callback.Status.ToLower() != "success")
            {
                payment.Status = 2; // Failed
                await _paymentRepository.UpdateAsync(payment);
                return false;
            }

            payment.Status = 1; // Completed
            payment.PaidAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);

            var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
            if (booking != null)
            {
                booking.Status = 1; // Confirmed
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(booking);
                
                await _emailService.SendBookingConfirmationAsync(booking.ContactEmail, booking);
            }

            _logger.LogInformation("Payment processed: {PaymentId}", paymentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return false;
        }
    }

    public async Task<PaymentResponse> GetPaymentStatusAsync(int paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new NotFoundException("Payment not found");
            }

            var statusString = payment.Status switch
            {
                0 => "Pending",
                1 => "Completed",
                2 => "Failed",
                3 => "Refunded",
                _ => "Unknown"
            };

            return new PaymentResponse
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                Status = statusString,
                Amount = payment.Amount,
                Provider = payment.Provider,
                TransactionRef = payment.TransactionRef,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status");
            throw;
        }
    }

    public async Task<List<PaymentHistoryResponse>> GetPaymentHistoryAsync(int bookingId)
    {
        try
        {
            var payments = await _paymentRepository.GetByBookingIdAsync(bookingId);
            var result = new List<PaymentHistoryResponse>();

            foreach (var payment in payments)
            {
                var statusString = payment.Status switch
                {
                    0 => "Pending",
                    1 => "Completed",
                    2 => "Failed",
                    3 => "Refunded",
                    _ => "Unknown"
                };

                result.Add(new PaymentHistoryResponse
                {
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    Status = statusString,
                    Provider = payment.Provider,
                    CreatedAt = payment.CreatedAt,
                    PaidAt = payment.PaidAt
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history");
            throw;
        }
    }
}
