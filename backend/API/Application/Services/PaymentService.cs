namespace API.Application.Services;

using API.Application.Dtos.Payment;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentService> _logger;
    private readonly MomoPaymentProvider _momoProvider;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IBookingPassengerRepository passengerRepository,
        IEmailService emailService,
        ILogger<PaymentService> logger,
        MomoPaymentProvider momoProvider)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _passengerRepository = passengerRepository;
        _emailService = emailService;
        _logger = logger;
        _momoProvider = momoProvider;
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
                Provider = "Momo",
                Method = "Momo",
                Amount = booking.FinalAmount,
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);

            var providerRequest = new PaymentProviderRequest
            {
                Amount = booking.FinalAmount,
                BookingId = bookingId,
                Email = booking.ContactEmail
            };

            var providerResponse = await _momoProvider.GeneratePaymentLinkAsync(providerRequest);
            createdPayment.TransactionRef = providerResponse.TransactionId;
            createdPayment.QrCodeData = providerResponse.QrCode;
            await _paymentRepository.UpdateAsync(createdPayment);

            _logger.LogInformation("Payment initiated with Momo: {PaymentId} for booking {BookingId}", createdPayment.Id, bookingId);

            return new PaymentResponse
            {
                PaymentId = createdPayment.Id,
                BookingId = bookingId,
                Status = "Pending",
                Amount = booking.FinalAmount,
                Provider = "Momo",
                TransactionRef = providerResponse.TransactionId,
                PaymentLink = providerResponse.PaymentLink,
                QrCode = providerResponse.QrCode,
                CreatedAt = createdPayment.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment with Momo");
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
                _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
                return false;
            }

            if (payment.Status != (int)PaymentStatus.Pending)
            {
                _logger.LogInformation(
                    "Duplicate callback for payment {PaymentId}: already processed. Returning success.",
                    paymentId);

                return payment.Status == (int)PaymentStatus.Completed;
            }

            if (callback.Status.ToLower() != "success")
            {
                payment.Status = (int)PaymentStatus.Failed;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                await ReleaseHeldSeatsForFailedPaymentAsync(payment.BookingId);

                _logger.LogWarning("Payment failed: {PaymentId}", paymentId);
                return false;
            }

            payment.Status = (int)PaymentStatus.Completed;
            payment.PaidAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);

            await ConfirmBookingAndConvertSeatsAsync(payment.BookingId);

            _logger.LogInformation("Payment processed successfully with Momo: {PaymentId} for booking {BookingId}", 
                paymentId, payment.BookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment with Momo");
            return false;
        }
    }

    private async Task ReleaseHeldSeatsForFailedPaymentAsync(int bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null) return;

            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
            if (passengers.Count == 0) return;

            var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                passengers.First().FlightSeatInventoryId);
            if (seatInventory == null) return;

            seatInventory.ReleaseHeldSeats(passengers.Count);
            await _seatInventoryRepository.UpdateAsync(seatInventory);

            _logger.LogInformation(
                "Released {PassengerCount} held seats after payment failure for booking {BookingId}",
                passengers.Count, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing held seats after payment failure for booking {BookingId}", bookingId);
        }
    }

    private async Task ConfirmBookingAndConvertSeatsAsync(int bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null) return;

            booking.Status = (int)BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);

            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
            if (passengers.Count == 0) return;

            var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                passengers.First().FlightSeatInventoryId);
            if (seatInventory == null) return;

            seatInventory.ConfirmHeldSeats(passengers.Count);
            await _seatInventoryRepository.UpdateAsync(seatInventory);

            await _emailService.SendBookingConfirmationAsync(booking.ContactEmail, booking);

            _logger.LogInformation(
                "Confirmed {PassengerCount} held seats as sold for booking {BookingId}",
                passengers.Count, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error confirming held seats as sold after payment success. Booking: {BookingId}",
                bookingId);
        }
    }

    public async Task<PaymentResponse> GetPaymentStatusAsync(int paymentId, int userId, bool isAdmin = false)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new NotFoundException("Payment not found");
            }

            var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
            if (booking == null)
            {
                throw new NotFoundException("Associated booking not found");
            }

            if (!isAdmin && booking.UserId != userId)
            {
                _logger.LogWarning(
                    "IDOR attempt: User {UserId} tried to access payment {PaymentId}",
                    userId, paymentId);
                throw new UnauthorizedException("You cannot access this payment");
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

    public async Task<List<PaymentHistoryResponse>> GetPaymentHistoryAsync(int bookingId, int userId, bool isAdmin = false)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            if (!isAdmin && booking.UserId != userId)
            {
                _logger.LogWarning(
                    "IDOR attempt: User {UserId} tried to access payment history for booking {BookingId}",
                    userId, bookingId);
                throw new UnauthorizedException("You cannot access this booking's payment history");
            }

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

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}
