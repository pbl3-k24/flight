namespace API.Application.Services;

using API.Application.Dtos.Payment;
using API.Application.Dtos.TicketUpgrade;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class TicketUpgradeService : ITicketUpgradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FlightBookingDbContext _dbContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPricingService _pricingService;
    private readonly VnpayPaymentProvider _vnpayProvider;
    private readonly ILogger<TicketUpgradeService> _logger;
    private readonly IConfiguration _configuration;

    public TicketUpgradeService(
        IUnitOfWork unitOfWork,
        FlightBookingDbContext dbContext,
        IPaymentRepository paymentRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IBookingRepository bookingRepository,
        IPricingService pricingService,
        VnpayPaymentProvider vnpayProvider,
        IConfiguration configuration,
        ILogger<TicketUpgradeService> logger)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _paymentRepository = paymentRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _bookingRepository = bookingRepository;
        _pricingService = pricingService;
        _vnpayProvider = vnpayProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TicketUpgradeQuoteResponseDto> GetQuoteAsync(int bookingId, int ticketId, int toSeatClassId, int userId)
    {
        var context = await BuildUpgradeContextAsync(bookingId, ticketId, toSeatClassId, userId);

        return new TicketUpgradeQuoteResponseDto
        {
            BookingId = bookingId,
            TicketId = ticketId,
            FromSeatClassId = context.Ticket.SeatClassId,
            ToSeatClassId = toSeatClassId,
            PaidAmountOfOldTicket = context.PaidAmountOfOldTicket,
            NewTicketAmount = context.NewTicketAmount,
            FareDifference = context.FareDifference,
            UpgradeFee = context.UpgradeFee,
            UpgradeAmount = context.UpgradeAmount,
            Currency = "VND"
        };
    }

    public async Task<bool> IsUpgradePaymentAsync(int paymentId)
    {
        return await _dbContext.TicketUpgradeRequests.AnyAsync(r =>
            !r.IsDeleted &&
            r.PaymentId == paymentId);
    }

    public async Task<TicketUpgradeRequestResponseDto> CreateRequestAsync(int bookingId, int ticketId, int toSeatClassId, int userId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var context = await BuildUpgradeContextAsync(bookingId, ticketId, toSeatClassId, userId);

            var now = DateTime.UtcNow;
            var hasPending = await _dbContext.TicketUpgradeRequests.AnyAsync(r =>
                !r.IsDeleted &&
                r.TicketId == ticketId &&
                r.Status == (int)TicketUpgradeStatus.Pending &&
                r.ExpiresAt > now);
            if (hasPending)
            {
                throw new ValidationException("A pending upgrade request already exists for this ticket");
            }

            var holdSucceeded = await _seatInventoryRepository.TryHoldSeatsAtomicAsync(context.TargetInventory.Id, 1);
            if (!holdSucceeded)
            {
                throw new ConcurrencyException("No available seats in target class for upgrade");
            }

            var request = new TicketUpgradeRequest
            {
                BookingId = bookingId,
                TicketId = ticketId,
                BookingPassengerId = context.Ticket.BookingPassengerId,
                FlightId = context.Ticket.FlightId,
                FromSeatClassId = context.Ticket.SeatClassId,
                ToSeatClassId = toSeatClassId,
                FromInventoryId = context.CurrentInventory.Id,
                ToInventoryId = context.TargetInventory.Id,
                PriceDifference = context.UpgradeAmount,
                Currency = "VND",
                Status = (int)TicketUpgradeStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _dbContext.TicketUpgradeRequests.AddAsync(request);
            await _dbContext.SaveChangesAsync();

            return MapRequest(request);
        });
    }

    public async Task<TicketUpgradePaymentResponseDto> InitiatePaymentAsync(int requestId, string paymentMethod, int userId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var request = await _dbContext.TicketUpgradeRequests.FirstOrDefaultAsync(r =>
                !r.IsDeleted && r.Id == requestId);
            if (request == null)
            {
                throw new NotFoundException("Upgrade request not found");
            }

            if (request.Status != (int)TicketUpgradeStatus.Pending)
            {
                throw new ValidationException("Only pending upgrade requests can be paid");
            }

            if (request.ExpiresAt <= DateTime.UtcNow)
            {
                throw new ValidationException("Upgrade request has expired");
            }

            var booking = await _bookingRepository.GetByIdAsync(request.BookingId);
            if (booking == null || booking.IsDeleted)
            {
                throw new NotFoundException("Booking not found");
            }

            if (booking.UserId != userId)
            {
                throw new UnauthorizedException("You cannot pay this upgrade request");
            }

            // Re-quote at payment time to guarantee latest seat-class price is used.
            var latestContext = await BuildUpgradeContextAsync(
                request.BookingId,
                request.TicketId,
                request.ToSeatClassId,
                userId);
            var latestUpgradeAmount = latestContext.UpgradeAmount;
            request.PriceDifference = latestUpgradeAmount;
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = userId;

            if (request.PaymentId.HasValue)
            {
                var existingPayment = await _paymentRepository.GetByIdAsync(request.PaymentId.Value);
                if (existingPayment != null && existingPayment.Status == (int)PaymentStatus.Pending)
                {
                    existingPayment.Status = (int)PaymentStatus.Failed;
                    existingPayment.UpdatedAt = DateTime.UtcNow;
                    await _paymentRepository.UpdateAsync(existingPayment);
                }
            }

            var normalizedMethod = string.IsNullOrWhiteSpace(paymentMethod)
                ? "VNPAY"
                : paymentMethod.Trim().ToUpperInvariant();
            if (normalizedMethod != "VNPAY")
            {
                throw new ValidationException("Payment method must be VNPAY");
            }

            var providerResponse = await _vnpayProvider.GeneratePaymentLinkAsync(new PaymentProviderRequest
            {
                Amount = latestUpgradeAmount,
                BookingId = booking.Id,
                Email = booking.ContactEmail,
                OrderDescription = $"Upgrade ticket {request.TicketId} booking {booking.BookingCode}"
            });

            var payment = new Payment
            {
                BookingId = booking.Id,
                Provider = normalizedMethod,
                Method = normalizedMethod,
                Amount = latestUpgradeAmount,
                Status = (int)PaymentStatus.Pending,
                TransactionRef = providerResponse.TransactionId,
                QrCodeData = providerResponse.QrCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);
            request.PaymentId = createdPayment.Id;
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = userId;
            await _dbContext.SaveChangesAsync();

            return new TicketUpgradePaymentResponseDto
            {
                RequestId = requestId,
                Payment = new PaymentResponse
                {
                    PaymentId = createdPayment.Id,
                    BookingId = booking.Id,
                    Status = "Pending",
                    Amount = createdPayment.Amount,
                    Provider = createdPayment.Provider,
                    TransactionRef = createdPayment.TransactionRef,
                    PaymentUrl = providerResponse.PaymentLink,
                    QrCode = providerResponse.QrCode ?? providerResponse.PaymentLink,
                    CreatedAt = createdPayment.CreatedAt,
                    ExpiresAt = providerResponse.ExpiresAt
                },
                PaymentId = createdPayment.Id,
                PaymentUrl = providerResponse.PaymentLink,
                QrCode = providerResponse.QrCode ?? providerResponse.PaymentLink,
                Amount = createdPayment.Amount
            };
        });
    }

    public async Task<bool> ProcessUpgradePaymentAsync(int paymentId, string paymentStatus)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var request = await _dbContext.TicketUpgradeRequests
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.PaymentId == paymentId);
            if (request == null)
            {
                return false;
            }

            if (request.Status != (int)TicketUpgradeStatus.Pending)
            {
                return request.Status == (int)TicketUpgradeStatus.Paid;
            }

            var isSuccess = string.Equals(paymentStatus, "success", StringComparison.OrdinalIgnoreCase);
            if (!isSuccess)
            {
                request.Status = request.ExpiresAt <= DateTime.UtcNow
                    ? (int)TicketUpgradeStatus.Expired
                    : (int)TicketUpgradeStatus.Failed;
                request.UpdatedAt = DateTime.UtcNow;

                await _seatInventoryRepository.TryReleaseHeldSeatsAtomicAsync(request.ToInventoryId, 1);
                await _dbContext.SaveChangesAsync();
                return false;
            }

            try
            {
                var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.TicketId);
                if (ticket == null || ticket.IsDeleted)
                {
                    throw new NotFoundException("Ticket not found");
                }

                var booking = await _bookingRepository.GetByIdAsync(request.BookingId);
                if (booking == null || booking.IsDeleted)
                {
                    throw new NotFoundException("Booking not found");
                }

                if (ticket.Status != 0)
                {
                    throw new ValidationException("Only issued tickets can be upgraded");
                }

                if (ticket.SeatClassId == request.ToSeatClassId)
                {
                    request.Status = (int)TicketUpgradeStatus.Paid;
                    request.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation(
                        "Upgrade already applied before callback completion. RequestId={RequestId}, TicketId={TicketId}",
                        request.Id,
                        ticket.Id);
                    return true;
                }

                var confirmed = await _seatInventoryRepository.TryConfirmHeldSeatsAtomicAsync(request.ToInventoryId, 1);
                if (!confirmed)
                {
                    throw new ConcurrencyException("Unable to confirm held upgrade seat");
                }

                var releasedOldSold = await _seatInventoryRepository.TryCancelSoldSeatsAtomicAsync(request.FromInventoryId, 1);
                if (!releasedOldSold)
                {
                    throw new ConcurrencyException("Unable to release original class sold seat");
                }

                var fareDelta = request.PriceDifference - GetUpgradeFee();
                if (fareDelta < 0)
                {
                    _logger.LogWarning(
                        "Computed fare delta is negative for upgrade request {RequestId}. PriceDifference={PriceDifference}",
                        request.Id,
                        request.PriceDifference);
                    fareDelta = 0;
                }

                ticket.SeatClassId = request.ToSeatClassId;
                ticket.Price += fareDelta;
                ticket.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Tickets.UpdateAsync(ticket);

                booking.TotalAmount += request.PriceDifference;
                booking.FinalAmount = booking.TotalAmount - booking.DiscountAmount;
                if (booking.FinalAmount < 0)
                {
                    throw new ValidationException("Booking final amount cannot be negative after ticket upgrade");
                }
                booking.UpdatedAt = DateTime.UtcNow;

                request.Status = (int)TicketUpgradeStatus.Paid;
                request.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Ticket upgrade applied. RequestId={RequestId}, TicketId={TicketId}", request.Id, ticket.Id);
                return true;
            }
            catch (Exception ex)
            {
                request.Status = (int)TicketUpgradeStatus.Failed;
                request.UpdatedAt = DateTime.UtcNow;
                await _seatInventoryRepository.TryReleaseHeldSeatsAtomicAsync(request.ToInventoryId, 1);
                await _dbContext.SaveChangesAsync();
                _logger.LogError(ex, "Ticket upgrade apply failed after payment completion. RequestId={RequestId}, PaymentId={PaymentId}", request.Id, paymentId);
                return false;
            }
        });
    }

    public async Task<int> ExpirePendingRequestsAsync()
    {
        var now = DateTime.UtcNow;
        var pending = await _dbContext.TicketUpgradeRequests
            .Where(r => !r.IsDeleted
                && r.Status == (int)TicketUpgradeStatus.Pending
                && r.ExpiresAt < now)
            .ToListAsync();

        var expired = 0;
        foreach (var request in pending)
        {
            var processed = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var tracked = await _dbContext.TicketUpgradeRequests.FirstOrDefaultAsync(r => r.Id == request.Id);
                if (tracked == null || tracked.Status != (int)TicketUpgradeStatus.Pending)
                {
                    return false;
                }

                tracked.Status = (int)TicketUpgradeStatus.Expired;
                tracked.UpdatedAt = DateTime.UtcNow;
                await _seatInventoryRepository.TryReleaseHeldSeatsAtomicAsync(tracked.ToInventoryId, 1);
                await _dbContext.SaveChangesAsync();
                return true;
            });

            if (processed)
            {
                expired++;
            }
        }

        return expired;
    }

    private async Task<UpgradeValidationContext> BuildUpgradeContextAsync(int bookingId, int ticketId, int toSeatClassId, int userId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null || booking.IsDeleted)
        {
            throw new NotFoundException("Booking not found");
        }

        if (booking.UserId != userId)
        {
            throw new UnauthorizedException("You cannot access this booking");
        }

        if (booking.Status != (int)BookingStatus.Confirmed)
        {
            throw new ValidationException("Only confirmed bookings can request ticket upgrades");
        }

        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
        if (ticket == null || ticket.IsDeleted || ticket.BookingId != bookingId)
        {
            throw new NotFoundException("Ticket not found in booking");
        }

        if (ticket.Status != 0)
        {
            throw new ValidationException("Only issued tickets can be upgraded");
        }

        var fromSeatClass = await _unitOfWork.SeatClasses.GetByIdAsync(ticket.SeatClassId);
        var toSeatClass = await _unitOfWork.SeatClasses.GetByIdAsync(toSeatClassId);
        if (fromSeatClass == null || toSeatClass == null || toSeatClass.IsDeleted)
        {
            throw new ValidationException("Invalid target seat class");
        }

        if (toSeatClass.Id == fromSeatClass.Id)
        {
            throw new ValidationException("Target seat class must be different from current class");
        }

        if (toSeatClass.Priority >= fromSeatClass.Priority)
        {
            throw new ValidationException("Target seat class must be higher than current class");
        }

        var currentInventory = await _seatInventoryRepository.GetByFlightAndSeatClassAsync(ticket.FlightId, fromSeatClass.Id);
        var targetInventory = await _seatInventoryRepository.GetByFlightAndSeatClassAsync(ticket.FlightId, toSeatClass.Id);
        if (currentInventory == null || targetInventory == null)
        {
            throw new ValidationException("Seat inventory not found for upgrade");
        }

        var paidAmountOfOldTicket = ticket.Price;
        var newTicketAmount = await _pricingService.CalculateCurrentPriceAsync(targetInventory.Id);
        var upgradeFee = GetUpgradeFee();
        var fareDifference = newTicketAmount - paidAmountOfOldTicket;
        var upgradeAmount = fareDifference + upgradeFee;
        if (upgradeAmount <= 0)
        {
            throw new ValidationException("Upgrade amount must be greater than zero");
        }

        return new UpgradeValidationContext(
            booking,
            ticket,
            currentInventory,
            targetInventory,
            paidAmountOfOldTicket,
            newTicketAmount,
            fareDifference,
            upgradeFee,
            upgradeAmount);
    }

    private decimal GetUpgradeFee()
    {
        var fee = _configuration.GetValue<decimal?>("TicketUpgrade:UpgradeFee")
                  ?? _configuration.GetValue<decimal?>("TicketUpgrade:FixedFee")
                  ?? 0m;
        return fee < 0 ? 0m : fee;
    }

    private static TicketUpgradeRequestResponseDto MapRequest(TicketUpgradeRequest request)
    {
        var status = request.Status switch
        {
            (int)TicketUpgradeStatus.Pending => "Pending",
            (int)TicketUpgradeStatus.Paid => "Paid",
            (int)TicketUpgradeStatus.Cancelled => "Cancelled",
            (int)TicketUpgradeStatus.Expired => "Expired",
            (int)TicketUpgradeStatus.Failed => "Failed",
            _ => "Unknown"
        };

        return new TicketUpgradeRequestResponseDto
        {
            RequestId = request.Id,
            BookingId = request.BookingId,
            TicketId = request.TicketId,
            FromSeatClassId = request.FromSeatClassId,
            ToSeatClassId = request.ToSeatClassId,
            PriceDifference = request.PriceDifference,
            Currency = request.Currency,
            Status = status,
            ExpiresAt = request.ExpiresAt
        };
    }

    private sealed record UpgradeValidationContext(
        Booking Booking,
        Ticket Ticket,
        FlightSeatInventory CurrentInventory,
        FlightSeatInventory TargetInventory,
        decimal PaidAmountOfOldTicket,
        decimal NewTicketAmount,
        decimal FareDifference,
        decimal UpgradeFee,
        decimal UpgradeAmount);
}
