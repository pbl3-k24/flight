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
    private readonly IPaymentProviderFactory _paymentProviderFactory;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IBookingPassengerRepository passengerRepository,
        IEmailService emailService,
        ILogger<PaymentService> logger,
        IPaymentProviderFactory paymentProviderFactory)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _passengerRepository = passengerRepository;
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
                _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
                return false;
            }

            // IDEMPOTENCY: Check if payment has already been processed
            // If status is not Pending (0), the callback was already processed
            // Return success without mutating state to ensure idempotency
            if (payment.Status != 0) // Not Pending
            {
                var statusString = payment.Status switch
                {
                    1 => "Completed",
                    2 => "Failed",
                    3 => "Refunded",
                    _ => $"Unknown({payment.Status})"
                };

                _logger.LogInformation(
                    "Duplicate/Repeated callback for payment {PaymentId}: already in status {Status}. " +
                    "Returning success without state mutation (idempotent)",
                    paymentId, statusString);

                // Return true if already completed (success is idempotent)
                // Return false if already failed (failure is also idempotent)
                // Return true for unknown statuses to acknowledge callback
                return payment.Status == 1 || payment.Status == 3 || payment.Status == 0;
            }

            // SECURITY: Validate callback before processing
            var validationResult = await ValidatePaymentCallbackAsync(payment, callback);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Payment callback validation failed for {PaymentId}: {Reason}",
                    paymentId, validationResult.Reason);

                // Mark payment as failed due to invalid callback
                payment.Status = 2; // Failed
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                return false;
            }

            // If payment failed, release any held seats
            if (callback.Status.ToLower() != "success")
            {
                payment.Status = 2; // Failed
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                // Release held seats back to available (Held -> Available)
                var failedBooking = await _bookingRepository.GetByIdAsync(payment.BookingId);
                if (failedBooking != null)
                {
                    var passengers = await _passengerRepository.GetByBookingIdAsync(failedBooking.Id);
                    if (passengers.Count > 0)
                    {
                        var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                            passengers.First().FlightSeatInventoryId);
                        if (seatInventory != null)
                        {
                            try
                            {
                                seatInventory.ReleaseHeldSeats(passengers.Count);
                                await _seatInventoryRepository.UpdateAsync(seatInventory);
                                _logger.LogInformation(
                                    "Released {PassengerCount} held seats after payment failure for booking {BookingId}",
                                    passengers.Count, payment.BookingId);
                            }
                            catch (InvalidOperationException ex)
                            {
                                _logger.LogError(ex, "Error releasing held seats after payment failure");
                            }
                        }
                    }
                }

                _logger.LogWarning("Payment failed: {PaymentId}", paymentId);
                return false;
            }

            // Payment succeeded - convert held seats to sold
            payment.Status = 1; // Completed
            payment.PaidAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);

            var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
            if (booking != null)
            {
                // Update booking status to Confirmed
                booking.Status = 1; // Confirmed
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(booking);

                // Convert held seats to sold (Held -> Sold)
                var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
                if (passengers.Count > 0)
                {
                    var seatInventory = await _seatInventoryRepository.GetByIdAsync(
                        passengers.First().FlightSeatInventoryId);
                    if (seatInventory != null)
                    {
                        try
                        {
                            seatInventory.ConfirmHeldSeats(passengers.Count);
                            await _seatInventoryRepository.UpdateAsync(seatInventory);
                            _logger.LogInformation(
                                "Confirmed {PassengerCount} held seats as sold for booking {BookingId}",
                                passengers.Count, booking.Id);
                        }
                        catch (InvalidOperationException ex)
                        {
                            _logger.LogError(ex, 
                                "Error confirming held seats as sold after payment success. Booking: {BookingId}",
                                booking.Id);
                            // Don't fail the payment if seat update fails - payment is already processed
                            // This is logged for manual intervention
                        }
                    }
                }

                await _emailService.SendBookingConfirmationAsync(booking.ContactEmail, booking);
            }

            _logger.LogInformation("Payment processed successfully: {PaymentId} for booking {BookingId}", 
                paymentId, payment.BookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return false;
        }
    }

    /// <summary>
    /// Validates payment callback authenticity and correctness
    /// Checks: Signature, Amount match, Transaction ID, Payment provider verification
    /// </summary>
    private async Task<CallbackValidationResult> ValidatePaymentCallbackAsync(Payment payment, PaymentCallbackDto callback)
    {
        try
        {
            // 1. Check if callback has required fields
            if (string.IsNullOrEmpty(callback.TransactionId))
            {
                return CallbackValidationResult.Fail("Missing TransactionId in callback");
            }

            // 2. Verify amount matches the original payment
            if (callback.Amount != payment.Amount)
            {
                return CallbackValidationResult.Fail(
                    $"Amount mismatch: callback={callback.Amount}, payment={payment.Amount}");
            }

            // 3. Verify transaction ID matches if it was recorded
            if (!string.IsNullOrEmpty(payment.TransactionRef) && 
                callback.TransactionId != payment.TransactionRef)
            {
                return CallbackValidationResult.Fail(
                    $"TransactionId mismatch: callback={callback.TransactionId}, payment={payment.TransactionRef}");
            }

            // 4. Verify signature using payment provider
            if (!string.IsNullOrEmpty(callback.Signature) && !string.IsNullOrEmpty(callback.RawData))
            {
                var provider = _paymentProviderFactory.CreateProvider(payment.Provider);
                var signatureValid = await provider.VerifyCallbackSignatureAsync(
                    callback.Signature, 
                    callback.RawData);

                if (!signatureValid)
                {
                    _logger.LogWarning(
                        "Signature verification failed for payment {PaymentId} from provider {Provider}",
                        payment.Id, payment.Provider);
                    return CallbackValidationResult.Fail("Invalid callback signature");
                }
            }
            else if (!string.IsNullOrEmpty(callback.Signature) || !string.IsNullOrEmpty(callback.RawData))
            {
                // If signature is present but not both signature and raw data, it's incomplete
                return CallbackValidationResult.Fail("Incomplete signature data for verification");
            }

            // 5. Additional provider-specific validation (e.g., verify with provider API)
            if (!string.IsNullOrEmpty(callback.TransactionId))
            {
                var provider = _paymentProviderFactory.CreateProvider(payment.Provider);
                var providerStatus = await provider.GetPaymentStatusAsync(callback.TransactionId);

                if (string.IsNullOrEmpty(providerStatus) || 
                    providerStatus.ToLower() != callback.Status.ToLower())
                {
                    _logger.LogWarning(
                        "Provider status mismatch for payment {PaymentId}: callback={Status}, provider={ProviderStatus}",
                        payment.Id, callback.Status, providerStatus);
                    return CallbackValidationResult.Fail(
                        "Payment status mismatch with provider");
                }
            }

            return CallbackValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment callback for payment {PaymentId}", payment.Id);
            return CallbackValidationResult.Fail($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Result of callback validation
    /// </summary>
    private class CallbackValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; } = "";

        public static CallbackValidationResult Success() => new() { IsValid = true };
        public static CallbackValidationResult Fail(string reason) => new() { IsValid = false, Reason = reason };
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

            // Check ownership: only payment owner or admin can view
            var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
            if (booking == null)
            {
                throw new NotFoundException("Associated booking not found");
            }

            if (!isAdmin && booking.UserId != userId)
            {
                _logger.LogWarning(
                    "IDOR attempt: User {UserId} tried to access payment {PaymentId} belonging to user {OwnerId}",
                    userId, paymentId, booking.UserId);
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
            // Check ownership: only booking owner or admin can view payment history
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            if (!isAdmin && booking.UserId != userId)
            {
                _logger.LogWarning(
                    "IDOR attempt: User {UserId} tried to access payment history for booking {BookingId} belonging to user {OwnerId}",
                    userId, bookingId, booking.UserId);
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
