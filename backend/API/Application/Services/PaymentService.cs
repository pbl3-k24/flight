namespace API.Application.Services;

using API.Application.Dtos.Payment;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentService> _logger;
    private readonly MomoPaymentProvider _momoProvider;
    private readonly VnpayPaymentProvider _vnpayProvider;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IBookingPassengerRepository passengerRepository,
        IEmailService emailService,
        ILogger<PaymentService> logger,
        MomoPaymentProvider momoProvider,
        VnpayPaymentProvider vnpayProvider)
    {
        _unitOfWork = unitOfWork;
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _passengerRepository = passengerRepository;
        _emailService = emailService;
        _logger = logger;
        _momoProvider = momoProvider;
        _vnpayProvider = vnpayProvider;
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

            if (booking.Status != (int)BookingStatus.Pending)
            {
                throw new ValidationException("Only pending bookings can be paid");
            }

            var paymentMethod = NormalizePaymentMethod(dto.PaymentMethod);
            var providerResponse = await GeneratePaymentProviderResponseAsync(paymentMethod, booking);

            var payment = new Payment
            {
                BookingId = bookingId,
                Provider = paymentMethod,
                Method = paymentMethod,
                Amount = booking.FinalAmount,
                Status = 0,
                TransactionRef = providerResponse.TransactionId,
                QrCodeData = providerResponse.QrCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);

            _logger.LogInformation("Payment initiated with {Provider}: {PaymentId} for booking {BookingId}",
                paymentMethod,
                createdPayment.Id,
                bookingId);

            return new PaymentResponse
            {
                PaymentId = createdPayment.Id,
                BookingId = bookingId,
                Status = "Pending",
                Amount = booking.FinalAmount,
                Provider = paymentMethod,
                TransactionRef = providerResponse.TransactionId,
                PaymentLink = providerResponse.PaymentLink,
                QrCode = providerResponse.QrCode ?? providerResponse.PaymentLink,
                CreatedAt = createdPayment.CreatedAt,
                ExpiresAt = providerResponse.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            throw;
        }
    }

    private async Task<PaymentProviderResponse> GeneratePaymentProviderResponseAsync(string paymentMethod, Booking booking)
    {
        var providerRequest = new PaymentProviderRequest
        {
            Amount = booking.FinalAmount,
            BookingId = booking.Id,
            Email = booking.ContactEmail,
            OrderDescription = $"Thanh toan booking {booking.BookingCode}"
        };

        return paymentMethod switch
        {
            "VNPAY" => await _vnpayProvider.GeneratePaymentLinkAsync(providerRequest),
            "MOMO" => await _momoProvider.GeneratePaymentLinkAsync(providerRequest),
            _ => throw new ValidationException("Unsupported payment method")
        };
    }

    private static string NormalizePaymentMethod(string paymentMethod)
    {
        var normalizedMethod = string.IsNullOrWhiteSpace(paymentMethod)
            ? "VNPAY"
            : paymentMethod.Trim().ToUpperInvariant();

        return normalizedMethod switch
        {
            "VNPAY" => normalizedMethod,
            "MOMO" => normalizedMethod,
            _ => throw new ValidationException("Payment method must be VNPAY or MOMO")
        };
    }

    public async Task<bool> ProcessPaymentAsync(int paymentId, PaymentCallbackDto callback)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
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

                if (!await ValidateCallbackAsync(payment, callback))
                {
                    _logger.LogWarning(
                        "Payment callback validation failed for payment {PaymentId}. TransactionId: {TransactionId}",
                        paymentId,
                        callback.TransactionId);
                    return false;
                }

                payment.RawCallbackData = ResolveRawCallbackData(callback);

                if (!string.Equals(callback.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    payment.Status = (int)PaymentStatus.Failed;
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _paymentRepository.UpdateAsync(payment);

                    await CancelPendingBookingAndReleaseHeldSeatsAsync(payment.BookingId);

                    _logger.LogWarning("Payment failed: {PaymentId}", paymentId);
                    return false;
                }

                payment.Status = (int)PaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                await ConfirmBookingAndConvertSeatsAsync(payment.BookingId);

                _logger.LogInformation(
                    "Payment processed successfully with Vnpay: {PaymentId} for booking {BookingId}",
                    paymentId,
                    payment.BookingId);

                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return false;
        }
    }

    private async Task<bool> ValidateCallbackAsync(Payment payment, PaymentCallbackDto callback)
    {
        if (callback == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(payment.TransactionRef)
            || string.IsNullOrWhiteSpace(callback.TransactionId)
            || !FixedTimeEquals(payment.TransactionRef.Trim(), callback.TransactionId.Trim()))
        {
            _logger.LogWarning(
                "Callback transaction mismatch for payment {PaymentId}. Expected: {Expected}, Received: {Received}",
                payment.Id,
                payment.TransactionRef,
                callback.TransactionId);
            return false;
        }

                if (!AmountsMatch(payment.Amount, callback.Amount, payment.Provider))
        {
            _logger.LogWarning(
                "Callback amount mismatch for payment {PaymentId}. Expected: {Expected}, Received: {Received}",
                payment.Id,
                payment.Amount,
                callback.Amount);
            return false;
        }

        if (string.IsNullOrWhiteSpace(callback.Signature))
        {
            _logger.LogWarning("Missing callback signature for payment {PaymentId}", payment.Id);
            return false;
        }

        var rawData = ResolveRawCallbackData(callback);
        if (string.IsNullOrWhiteSpace(rawData))
        {
            _logger.LogWarning("Missing raw callback data for payment {PaymentId}", payment.Id);
            return false;
        }

        var provider = ResolvePaymentProvider(payment.Provider);
        var signatureValid = await provider.VerifyCallbackSignatureAsync(callback.Signature, rawData);
        if (!signatureValid)
        {
            _logger.LogWarning("Invalid callback signature for payment {PaymentId}", payment.Id);
            return false;
        }

        return true;
    }

    private IPaymentProvider ResolvePaymentProvider(string provider)
    {
        return provider.ToUpperInvariant() switch
        {
            "MOMO" => _momoProvider,
            "VNPAY" => _vnpayProvider,
            _ => throw new ValidationException("Unsupported payment provider")
        };
    }

    private static string? ResolveRawCallbackData(PaymentCallbackDto callback)
    {
        if (!string.IsNullOrWhiteSpace(callback.RawData))
        {
            return callback.RawData;
        }

        if (callback.AdditionalData is { Count: > 0 })
        {
            return string.Join("&", callback.AdditionalData
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}={kv.Value}"));
        }

        return null;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static bool AmountsMatch(decimal expectedAmount, decimal callbackAmount, string provider)
    {
        if (string.Equals(provider, "VNPAY", StringComparison.OrdinalIgnoreCase))
        {
            // VNPAY callback trả về số tiền thực (VND), so sánh trực tiếp
            // Cho phép sai lệch nhỏ do làm tròn
            return Math.Abs(callbackAmount - expectedAmount) < 1m;
        }

        return callbackAmount == expectedAmount;
    }

    private async Task CancelPendingBookingAndReleaseHeldSeatsAsync(int bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null || booking.Status != (int)BookingStatus.Pending)
        {
            return;
        }

        var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
        if (passengers.Count == 0)
        {
            booking.Status = (int)BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);
            return;
        }

        var seatInventory = await _seatInventoryRepository.GetByIdAsync(
            passengers.First().FlightSeatInventoryId);
        if (seatInventory != null)
        {
            seatInventory.ReleaseHeldSeats(passengers.Count);
            await _seatInventoryRepository.UpdateAsync(seatInventory);
        }

        booking.Status = (int)BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation(
            "Cancelled pending booking and released {PassengerCount} held seats after payment failure for booking {BookingId}",
            passengers.Count,
            bookingId);
    }

    private async Task ConfirmBookingAndConvertSeatsAsync(int bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return;
        }

        if (booking.Status == (int)BookingStatus.Confirmed)
        {
            return;
        }

        if (booking.Status != (int)BookingStatus.Pending)
        {
            _logger.LogWarning(
                "Skipping seat confirmation because booking {BookingId} is not pending. Current status: {Status}",
                bookingId,
                booking.Status);
            return;
        }

        var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
        if (passengers.Count == 0)
        {
            return;
        }

        var seatInventory = await _seatInventoryRepository.GetByIdAsync(
            passengers.First().FlightSeatInventoryId);
        if (seatInventory == null)
        {
            throw new NotFoundException("Seat inventory not found for booking");
        }

        seatInventory.ConfirmHeldSeats(passengers.Count);
        await _seatInventoryRepository.UpdateAsync(seatInventory);

        booking.Status = (int)BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        try
        {
            await _emailService.SendBookingConfirmationAsync(booking.ContactEmail, booking);
        }
        catch (Exception ex)
        {
            // Bắt lỗi gửi mail để không làm rollback transaction của thanh toán
            _logger.LogWarning(ex, "Payment succeeded but failed to send confirmation email for booking {BookingId}", bookingId);
        }

        _logger.LogInformation(
            "Confirmed {PassengerCount} held seats as sold for booking {BookingId}",
            passengers.Count,
            bookingId);
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

    public async Task<bool> ProcessVnpayCallbackAsync(PaymentCallbackDto callback)
    {
        try
        {
            // Tìm payment bằng TransactionRef
            var payments = await _paymentRepository.GetAllAsync();
            var payment = payments.FirstOrDefault(p => p.TransactionRef == callback.TransactionId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for TransactionRef: {TransactionRef}", callback.TransactionId);
                return false;
            }

            return await ProcessPaymentAsync(payment.Id, callback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPAY callback");
            return false;
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
