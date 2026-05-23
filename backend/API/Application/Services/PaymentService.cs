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
    private readonly ITicketService _ticketService;
    private readonly ILogger<PaymentService> _logger;
    private readonly VnpayPaymentProvider _vnpayProvider;
    private readonly INotificationService? _notificationService;
    private readonly IPromotionService? _promotionService;
    private readonly ITicketUpgradeService? _ticketUpgradeService;
    private readonly IBookingService? _bookingService;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IBookingPassengerRepository passengerRepository,
        IEmailService emailService,
        ITicketService ticketService,
        ILogger<PaymentService> logger,
        VnpayPaymentProvider vnpayProvider,
        INotificationService? notificationService = null,
        IPromotionService? promotionService = null,
        ITicketUpgradeService? ticketUpgradeService = null,
        IBookingService? bookingService = null)
    {
        _unitOfWork = unitOfWork;
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _passengerRepository = passengerRepository;
        _emailService = emailService;
        _ticketService = ticketService;
        _logger = logger;
        _vnpayProvider = vnpayProvider;
        _notificationService = notificationService;
        _promotionService = promotionService;
        _ticketUpgradeService = ticketUpgradeService;
        _bookingService = bookingService;
    }

    public async Task<PaymentResponse> InitiatePaymentAsync(int bookingId, InitiatePaymentDto dto, int userId, bool isAdmin = false)
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
                    "IDOR attempt: User {UserId} tried to initiate payment for booking {BookingId}",
                    userId,
                    bookingId);
                throw new UnauthorizedException("You cannot initiate payment for this booking");
            }

            if (booking.Status != (int)BookingStatus.Pending)
            {
                throw new ValidationException("Only pending bookings can be paid");
            }

            _logger.LogInformation(
                "InitiatePayment amount snapshot: BookingId={BookingId}, BookingCode={BookingCode}, TotalAmount={TotalAmount}, DiscountAmount={DiscountAmount}, FinalAmount={FinalAmount}, Currency={Currency}",
                booking.Id,
                booking.BookingCode,
                booking.TotalAmount,
                booking.DiscountAmount,
                booking.FinalAmount,
                booking.Currency);

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
                PaymentUrl = providerResponse.PaymentLink,
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
            _ => throw new ValidationException("Payment method must be VNPAY")
        };
    }

    public async Task<bool> ProcessPaymentAsync(int paymentId, PaymentCallbackDto callback)
    {
        try
        {
            int? confirmedBookingId = null;
            var processed = await _unitOfWork.ExecuteInTransactionAsync(async () =>
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

                if (!IsSuccessfulPaymentStatus(callback.Status))
                {
                    payment.Status = (int)PaymentStatus.Failed;
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _paymentRepository.UpdateAsync(payment);

                    if (_ticketUpgradeService != null
                        && await _ticketUpgradeService.IsUpgradePaymentAsync(payment.Id))
                    {
                        await _ticketUpgradeService.ProcessUpgradePaymentAsync(payment.Id, callback.Status);
                        _logger.LogInformation("Processed failed upgrade payment {PaymentId}", payment.Id);
                        return false;
                    }

                    if (_bookingService != null
                        && await _bookingService.IsChangeFlightPaymentAsync(payment.Id))
                    {
                        await _bookingService.ProcessChangeFlightPaymentAsync(payment.Id, callback.Status);
                        _logger.LogInformation("Processed failed change-flight payment {PaymentId}", payment.Id);
                        return false;
                    }

                    await CancelPendingBookingAndReleaseHeldSeatsAsync(payment.BookingId);

                    _logger.LogWarning("Payment failed: {PaymentId}", paymentId);
                    return false;
                }

                payment.Status = (int)PaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                if (_ticketUpgradeService != null
                    && await _ticketUpgradeService.IsUpgradePaymentAsync(payment.Id))
                {
                    var upgradeProcessed = await _ticketUpgradeService.ProcessUpgradePaymentAsync(payment.Id, callback.Status);
                    if (!upgradeProcessed)
                    {
                        payment.Status = (int)PaymentStatus.Failed;
                        payment.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepository.UpdateAsync(payment);
                    }
                    _logger.LogInformation("Upgrade payment processed: {PaymentId}, Success={Success}", payment.Id, upgradeProcessed);
                    return upgradeProcessed;
                }

                if (_bookingService != null
                    && await _bookingService.IsChangeFlightPaymentAsync(payment.Id))
                {
                    var changeProcessed = await _bookingService.ProcessChangeFlightPaymentAsync(payment.Id, callback.Status);
                    if (!changeProcessed)
                    {
                        payment.Status = (int)PaymentStatus.Failed;
                        payment.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepository.UpdateAsync(payment);
                    }
                    _logger.LogInformation("Change-flight payment processed: {PaymentId}, Success={Success}", payment.Id, changeProcessed);
                    return changeProcessed;
                }

                if (await ConfirmBookingAndConvertSeatsAsync(payment.BookingId))
                {
                    confirmedBookingId = payment.BookingId;
                }

                _logger.LogInformation(
                    "Payment processed successfully with Vnpay: {PaymentId} for booking {BookingId}",
                    paymentId,
                    payment.BookingId);

                return true;
            });

            if (processed && confirmedBookingId.HasValue)
            {
                await NotifyPaymentSuccessAsync(paymentId, confirmedBookingId.Value);
            }

            return processed;
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

        var amountMatched = AmountsMatch(payment.Amount, callback.Amount, payment.Provider);
        if (!amountMatched)
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
        _logger.LogInformation(
            "Callback validation diagnostic. PaymentId={PaymentId}, TxnRef={TxnRef}, AmountExpected={AmountExpected}, AmountActual={AmountActual}, SignatureValid={SignatureValid}",
            payment.Id,
            payment.TransactionRef,
            payment.Amount,
            callback.Amount,
            signatureValid);

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
            "VNPAY" => _vnpayProvider,
            _ => throw new ValidationException("Unsupported payment provider")
        };
    }

    private static string? ResolveRawCallbackData(PaymentCallbackDto callback)
    {
        if (!string.IsNullOrWhiteSpace(callback.RawData))
        {
            return callback.RawData.Trim();
        }

        if (callback.AdditionalData is { Count: > 0 })
        {
            var filtered = callback.AdditionalData
                .Where(kv =>
                    !string.IsNullOrWhiteSpace(kv.Key)
                    && !string.IsNullOrWhiteSpace(kv.Value)
                    && !string.Equals(kv.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(kv.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{System.Net.WebUtility.UrlEncode(kv.Key)}={System.Net.WebUtility.UrlEncode(kv.Value)}");
            return string.Join("&", filtered);
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

        var seatInventoryId = passengers.First().FlightSeatInventoryId;
        var seatsToRelease = passengers.Count(p => p.PassengerType != (int)PassengerType.Infant);
        if (seatsToRelease > 0)
        {
            var released = await _seatInventoryRepository.TryReleaseHeldSeatsAtomicAsync(seatInventoryId, seatsToRelease);
            if (!released)
            {
                throw new ConcurrencyException("Unable to release held seats due to concurrent updates. Please retry.");
            }
        }

        booking.Status = (int)BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation(
            "Cancelled pending booking and released {PassengerCount} held seats after payment failure for booking {BookingId}",
            seatsToRelease,
            bookingId);
    }

    private async Task<bool> ConfirmBookingAndConvertSeatsAsync(int bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        if (booking.Status == (int)BookingStatus.Confirmed)
        {
            return false;
        }

        if (booking.Status != (int)BookingStatus.Pending)
        {
            _logger.LogWarning(
                "Skipping seat confirmation because booking {BookingId} is not pending. Current status: {Status}",
                bookingId,
                booking.Status);
            return false;
        }

        var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
        if (passengers.Count == 0)
        {
            return false;
        }

        var seatInventoryId = passengers.First().FlightSeatInventoryId;
        var seatsToConfirm = passengers.Count(p => p.PassengerType != (int)PassengerType.Infant);
        if (seatsToConfirm > 0)
        {
            var confirmed = await _seatInventoryRepository.TryConfirmHeldSeatsAtomicAsync(seatInventoryId, seatsToConfirm);
            if (!confirmed)
            {
                throw new ConcurrencyException("Unable to confirm held seats due to concurrent updates. Please retry.");
            }
        }

        booking.Status = (int)BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        try
        {
            await _ticketService.CreateTicketsAsync(bookingId);
            await _emailService.SendBookingConfirmationAsync(booking.ContactEmail, booking);
        }
        catch (Exception ex)
        {
            // Bắt lỗi gửi mail hoặc tạo vé để không làm rollback transaction của thanh toán
            _logger.LogWarning(ex, "Payment succeeded but failed to generate tickets or send confirmation email for booking {BookingId}", bookingId);
        }

        _logger.LogInformation(
            "Confirmed {PassengerCount} held seats as sold for booking {BookingId}",
            seatsToConfirm,
            bookingId);
        return true;
    }

    private async Task NotifyPaymentSuccessAsync(int paymentId, int bookingId)
    {
        if (_notificationService == null)
        {
            return;
        }

        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return;
        }

        await _notificationService.SendNotificationAsync(
            booking.UserId,
            "Payment successful",
            $"Payment for booking {booking.BookingCode} has been completed.",
            type: "IN_APP",
            category: "PAYMENT",
            relatedEntityType: "Payment",
            relatedEntityId: paymentId);
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
                4 => "RefundFailed",
                5 => "PendingRefund",
                6 => "PartialRefunded",
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
                    4 => "RefundFailed",
                    5 => "PendingRefund",
                    6 => "PartialRefunded",
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
            var callbackTransactionId = callback.TransactionId?.Trim();
            var payment = payments.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(p.TransactionRef)
                && string.Equals(
                    p.TransactionRef.Trim(),
                    callbackTransactionId,
                    StringComparison.OrdinalIgnoreCase));

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

    private static bool IsSuccessfulPaymentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim();
        return normalized.Equals("success", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("completed", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("paid", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("00", StringComparison.OrdinalIgnoreCase);
    }
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3,
    RefundFailed = 4,
    PendingRefund = 5,
    PartialRefunded = 6
}
