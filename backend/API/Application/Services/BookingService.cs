                                     namespace API.Application.Services;

using API.Application.Dtos.Booking;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Common;
using API.Domain.Entities;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using BookingServiceEntity = API.Domain.Entities.BookingService;

public class BookingService : IBookingService
{
    private const int ChangeFlightPaymentHoldMinutes = 5;
    private const decimal ChildFareRate = 0.7m;
    private const decimal InfantFlatFare = 100000m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricingService;
    private readonly IPromotionService _promotionService;
    private readonly ILogger<BookingService> _logger;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly FlightBookingDbContext _dbContext;
    private readonly VnpayPaymentProvider _vnpayPaymentProvider;
    private readonly INotificationService? _notificationService;
    private readonly IConfiguration _configuration;

    public BookingService(
        IUnitOfWork unitOfWork,
        IPricingService pricingService,
        IPromotionService promotionService,
        IBackgroundJobService backgroundJobService,
        FlightBookingDbContext dbContext,
        VnpayPaymentProvider vnpayPaymentProvider,
        ILogger<BookingService> logger,
        IConfiguration configuration,
        INotificationService? notificationService = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
        _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
        _backgroundJobService = backgroundJobService ?? throw new ArgumentNullException(nameof(backgroundJobService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _vnpayPaymentProvider = vnpayPaymentProvider ?? throw new ArgumentNullException(nameof(vnpayPaymentProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _notificationService = notificationService;
    }

    private async Task<int> ResolveFlightIdAsync(string flightNumber, DateTime departureDate)
    {
        var normalized = flightNumber.Trim().ToUpperInvariant();
        var start = departureDate.Date;
        var end = start.AddDays(1);

        var flights = await _unitOfWork.Flights.GetAllAsync();
        var flight = flights.FirstOrDefault(f =>
            string.Equals(f.FlightNumber, normalized, StringComparison.OrdinalIgnoreCase)
            && f.DepartureTime >= start
            && f.DepartureTime < end);

        if (flight == null)
        {
            throw new NotFoundException("Flight not found for provided number and date");
        }

        return flight.Id;
    }

    public async Task<BookingResponse> CreateBookingAsync(int userId, CreateBookingDto dto)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var outboundFlightId = dto.OutboundFlightId;
                if (outboundFlightId <= 0
                    && !string.IsNullOrWhiteSpace(dto.OutboundFlightNumber)
                    && dto.OutboundDepartureDate.HasValue)
                {
                    outboundFlightId = await ResolveFlightIdAsync(
                        dto.OutboundFlightNumber,
                        dto.OutboundDepartureDate.Value);
                }

                // 1. Validate flight exists
                var outboundFlight = await _unitOfWork.Flights.GetByIdAsync(outboundFlightId);
                if (outboundFlight == null)
                {
                    throw new NotFoundException("Flight not found");
                }

                if (outboundFlight.Status != 0)
                {
                    throw new ValidationException("Selected flight is not available for booking");
                }

                if (dto.OutboundDepartureDate.HasValue)
                {
                    var selectedDateVn = VietnamTime.GetVietnamDate(dto.OutboundDepartureDate.Value);
                    var flightDepartureDateVn = VietnamTime.ToVietnamTime(outboundFlight.DepartureTime).Date;
                    if (selectedDateVn != flightDepartureDateVn)
                    {
                        _logger.LogWarning(
                            "OutboundDepartureDate mismatch ignored. Booking will trust OutboundFlightId. FlightId={FlightId}, RequestDateVn={RequestDateVn}, FlightDateVn={FlightDateVn}",
                            outboundFlight.Id,
                            selectedDateVn,
                            flightDepartureDateVn);
                    }
                }

                // Allow booking up to 2 hours before departure
                var minimumBookingTime = DateTime.UtcNow.AddHours(2);
                if (outboundFlight.DepartureTime <= minimumBookingTime)
                {
                    throw new ValidationException("Cannot book flights departing within 2 hours");
                }

                if (dto.ReturnFlightId.HasValue)
                {
                    var returnFlight = await _unitOfWork.Flights.GetByIdAsync(dto.ReturnFlightId.Value);
                    if (returnFlight == null)
                    {
                        throw new NotFoundException("Return flight not found");
                    }

                    if (returnFlight.Status != 0)
                    {
                        throw new ValidationException("Selected return flight is not available for booking");
                    }

                    if (returnFlight.DepartureTime <= outboundFlight.ArrivalTime)
                    {
                        throw new ValidationException("Return flight must depart after the outbound flight arrives");
                    }
                }
                else if (!dto.ReturnFlightId.HasValue
                         && !string.IsNullOrWhiteSpace(dto.ReturnFlightNumber)
                         && dto.ReturnDepartureDate.HasValue)
                {
                    var returnFlightId = await ResolveFlightIdAsync(
                        dto.ReturnFlightNumber,
                        dto.ReturnDepartureDate.Value);
                    var returnFlight = await _unitOfWork.Flights.GetByIdAsync(returnFlightId);
                    if (returnFlight == null)
                    {
                        throw new NotFoundException("Return flight not found");
                    }

                    if (returnFlight.Status != 0)
                    {
                        throw new ValidationException("Selected return flight is not available for booking");
                    }

                    if (returnFlight.DepartureTime <= outboundFlight.ArrivalTime)
                    {
                        throw new ValidationException("Return flight must depart after the outbound flight arrives");
                    }

                    dto.ReturnFlightId = returnFlightId;
                }

                // 2. Validate passenger count
                if (dto.Passengers.Count != dto.PassengerCount || dto.PassengerCount <= 0 || dto.PassengerCount > 9)
                {
                    throw new ValidationException("Invalid passenger count");
                }

                var passengerProfiles = dto.Passengers
                    .Select(p => new
                    {
                        Passenger = p,
                        Type = ResolvePassengerType(p.DateOfBirth, outboundFlight.DepartureTime)
                    })
                    .ToList();

                var hasPassengerAtLeast14 = passengerProfiles.Any(p =>
                    CalculateAgeAtDeparture(p.Passenger.DateOfBirth, outboundFlight.DepartureTime) >= 14);
                if (!hasPassengerAtLeast14)
                {
                    throw new ValidationException("A booking must include at least one passenger aged 14 or older");
                }

                // 3. Validate seats available for each leg (infants do not consume seats)
                var outboundInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(
                    outboundFlightId, dto.SeatClassId);
                var seatsToHold = passengerProfiles.Count(p => p.Type != PassengerType.Infant);
                if (outboundInventory == null || outboundInventory.AvailableSeats < seatsToHold)
                {
                    throw new ValidationException("Insufficient seats available for outbound flight");
                }

                FlightSeatInventory? returnInventory = null;
                if (dto.ReturnFlightId.HasValue)
                {
                    returnInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(
                        dto.ReturnFlightId.Value,
                        dto.SeatClassId);
                    if (returnInventory == null || returnInventory.AvailableSeats < seatsToHold)
                    {
                        throw new ValidationException("Insufficient seats available for return flight");
                    }
                }

                var outboundSeatPrice = await _pricingService.CalculateCurrentPriceAsync(outboundInventory.Id);
                var returnSeatPrice = returnInventory != null
                    ? await _pricingService.CalculateCurrentPriceAsync(returnInventory.Id)
                    : 0m;

                // 4. Calculate total amount with seat class pricing and additional services
                var includedServiceIds = await _dbContext.ClassServiceConfigs
                    .Where(c => c.SeatClassId == dto.SeatClassId && c.IsIncluded)
                    .Select(c => c.AdditionalServiceId)
                    .ToListAsync();
                var optionalServiceIds = await _dbContext.ClassServiceConfigs
                    .Where(c => c.SeatClassId == dto.SeatClassId && !c.IsIncluded)
                    .Select(c => c.AdditionalServiceId)
                    .ToHashSetAsync();

                var allAdditionalServices = await _dbContext.AdditionalServices
                    .Where(s => !s.IsDeleted)
                    .ToDictionaryAsync(s => s.Id, s => s);

                static List<PassengerServiceDto> NormalizeServices(List<PassengerServiceDto>? services)
                {
                    if (services == null)
                    {
                        return [];
                    }

                    return services
                        .GroupBy(s => s.AdditionalServiceId)
                        .Select(g => new PassengerServiceDto
                        {
                            AdditionalServiceId = g.Key,
                            Quantity = g.Sum(x => x.Quantity)
                        })
                        .ToList();
                }

                decimal additionalServicesTotal = 0;
                decimal passengerFareTotal = 0;
                foreach (var profile in passengerProfiles)
                {
                    var outboundFare = profile.Type switch
                    {
                        PassengerType.Infant => 0m,
                        PassengerType.Child => outboundSeatPrice * ChildFareRate,
                        _ => outboundSeatPrice
                    };
                    var returnFare = profile.Type switch
                    {
                        PassengerType.Infant => 0m,
                        PassengerType.Child => returnSeatPrice * ChildFareRate,
                        _ => returnSeatPrice
                    };
                    passengerFareTotal += outboundFare + returnFare;

                    var outboundOptionalServices = NormalizeServices(profile.Passenger.OptionalServices);
                    var returnOptionalServices = dto.ReturnFlightId.HasValue
                        ? NormalizeServices(profile.Passenger.ReturnOptionalServices)
                        : [];

                    if (profile.Type == PassengerType.Infant &&
                        (outboundOptionalServices.Count > 0 || returnOptionalServices.Count > 0))
                    {
                        throw new ValidationException("Infant passengers cannot use optional services");
                    }

                    foreach (var optSvc in outboundOptionalServices.Concat(returnOptionalServices))
                    {
                        ValidatePassengerServiceQuantity(optSvc.Quantity);

                        if (!allAdditionalServices.TryGetValue(optSvc.AdditionalServiceId, out var svc))
                        {
                            throw new ValidationException($"Additional service {optSvc.AdditionalServiceId} is invalid or unavailable");
                        }

                        if (includedServiceIds.Contains(optSvc.AdditionalServiceId))
                        {
                            throw new ValidationException($"Service {optSvc.AdditionalServiceId} is already included in the selected seat class");
                        }
                        if (!optionalServiceIds.Contains(optSvc.AdditionalServiceId))
                        {
                            throw new ValidationException($"Service {optSvc.AdditionalServiceId} is not available for the selected seat class");
                        }

                        additionalServicesTotal += svc.Price * optSvc.Quantity;
                    }
                }

                var totalAmount = passengerFareTotal + additionalServicesTotal;
                var adultCount = passengerProfiles.Count(p => p.Type == PassengerType.Adult);
                var childCount = passengerProfiles.Count(p => p.Type == PassengerType.Child);
                var infantCount = passengerProfiles.Count(p => p.Type == PassengerType.Infant);

                Promotion? promotion = null;
                decimal discountAmount = 0;

                if (!string.IsNullOrWhiteSpace(dto.PromotionCode))
                {
                    promotion = await _promotionService.ValidatePromotionCodeAsync(dto.PromotionCode.Trim());
                    if (promotion == null)
                    {
                        throw new ValidationException("Invalid or expired promotion code");
                    }
                }
                else if (dto.PromotionId.HasValue)
                {
                    promotion = await _unitOfWork.Promotions.GetByIdAsync(dto.PromotionId.Value);
                    if (promotion == null || !promotion.IsValid(DateTime.UtcNow) || !promotion.IsAvailable())
                    {
                        throw new ValidationException("Invalid or expired promotion code");
                    }
                }

                if (promotion != null)
                {
                    if (totalAmount < promotion.MinimumAmount)
                    {
                        throw new ValidationException("Booking total does not meet promotion minimum amount");
                    }

                    var alreadyUsed = await _dbContext.PromotionUsages.AnyAsync(pu =>
                        pu.PromotionId == promotion.Id && pu.UserId == userId);
                    if (alreadyUsed)
                    {
                        throw new ValidationException("This promotion has already been used by this account");
                    }

                    discountAmount = promotion.CalculateDiscount(totalAmount);
                    if (discountAmount <= 0)
                    {
                        throw new ValidationException("Promotion discount must be greater than 0");
                    }

                    if (discountAmount >= totalAmount)
                    {
                        throw new ValidationException("Promotion discount cannot be greater than or equal to booking total");
                    }

                    var reserved = await _unitOfWork.Promotions.TryReserveUsageAsync(promotion.Id);
                    if (!reserved)
                    {
                        throw new ValidationException("Promotion is no longer available");
                    }
                }

                _logger.LogInformation(
                    "Booking pricing breakdown: BookingCodeTemp={BookingCodeTemp}, UserId={UserId}, OutboundFlightId={OutboundFlightId}, ReturnFlightId={ReturnFlightId}, SeatClassId={SeatClassId}, Adult={AdultCount}, Child={ChildCount}, Infant={InfantCount}, OutboundUnitPrice={OutboundUnitPrice}, ReturnUnitPrice={ReturnUnitPrice}, PassengerFareTotal={PassengerFareTotal}, AdditionalServicesTotal={AdditionalServicesTotal}, DiscountAmount={DiscountAmount}, FinalAmount={FinalAmount}",
                    "PENDING",
                    userId,
                    outboundFlightId,
                    dto.ReturnFlightId,
                    dto.SeatClassId,
                    adultCount,
                    childCount,
                    infantCount,
                    outboundSeatPrice,
                    returnSeatPrice,
                    passengerFareTotal,
                    additionalServicesTotal,
                    discountAmount,
                    totalAmount - discountAmount);

                // 5. Create booking with expiration (1 hour timeout)
                var booking = new Booking
                {
                    UserId = userId,
                    BookingCode = GenerateBookingCode(),
                    OutboundFlightId = outboundFlightId,
                    ReturnFlightId = dto.ReturnFlightId,
                    Status = (int)BookingStatus.Pending,
                    ContactEmail = dto.ContactEmail ?? "",
                    TotalAmount = totalAmount,
                    DiscountAmount = discountAmount,
                    FinalAmount = totalAmount - discountAmount,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    PromotionId = promotion?.Id
                };

                var createdBooking = await _unitOfWork.Bookings.CreateAsync(booking);

                // 6. Create passengers
                var createdPassengers = new List<BookingPassenger>();
                foreach (var profile in passengerProfiles)
                {
                    var passengerDto = profile.Passenger;
                    var passenger = new BookingPassenger
                    {
                        BookingId = createdBooking.Id,
                        FirstName = passengerDto.FirstName,
                        LastName = passengerDto.LastName,
                        FullName = $"{passengerDto.FirstName} {passengerDto.LastName}".Trim(),
                        Email = passengerDto.Email,
                        Phone = passengerDto.Phone,
                        DateOfBirth = passengerDto.DateOfBirth,
                        Nationality = passengerDto.Nationality,
                        PassportNumber = passengerDto.PassportNumber,
                        PassengerType = (int)profile.Type,
                        DocumentCheckStatus = ResolveInitialDocumentCheckStatus(profile.Type),
                        FlightSeatInventoryId = outboundInventory.Id,
                        FareSnapshot = BuildFareSnapshot(
                            outboundFlightId,
                            dto.ReturnFlightId,
                            profile.Type,
                            outboundSeatPrice,
                            returnSeatPrice)
                    };

                    await _unitOfWork.BookingPassengers.CreateAsync(passenger);
                    createdPassengers.Add(passenger);
                }

                // 6.1 Create legs
                var outboundLeg = new BookingLeg
                {
                    BookingId = createdBooking.Id,
                    FlightId = outboundFlightId,
                    SeatClassId = dto.SeatClassId,
                    LegType = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Set<BookingLeg>().Add(outboundLeg);

                BookingLeg? returnLeg = null;
                if (dto.ReturnFlightId.HasValue)
                {
                    returnLeg = new BookingLeg
                    {
                        BookingId = createdBooking.Id,
                        FlightId = dto.ReturnFlightId.Value,
                        SeatClassId = dto.SeatClassId,
                        LegType = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.Set<BookingLeg>().Add(returnLeg);
                }
                await _dbContext.SaveChangesAsync();

                // 6.2 Create leg-passengers + services per leg
                for (var i = 0; i < passengerProfiles.Count; i++)
                {
                    var profile = passengerProfiles[i];
                    var passenger = createdPassengers[i];

                    var outboundLegPassenger = new BookingLegPassenger
                    {
                        BookingLegId = outboundLeg.Id,
                        BookingPassengerId = passenger.Id,
                        FlightSeatInventoryId = outboundInventory.Id,
                        DocumentCheckStatus = ResolveInitialDocumentCheckStatus(profile.Type),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.Set<BookingLegPassenger>().Add(outboundLegPassenger);
                    await _dbContext.SaveChangesAsync();

                    if (profile.Type != PassengerType.Infant)
                    {
                        foreach (var includedId in includedServiceIds)
                        {
                            _dbContext.BookingServices.Add(new API.Domain.Entities.BookingService
                            {
                                BookingPassengerId = passenger.Id,
                                BookingLegPassengerId = outboundLegPassenger.Id,
                                AdditionalServiceId = includedId,
                                Quantity = 1,
                                Price = 0
                            });
                        }
                    }

                    var outboundOptionalServices = NormalizeServices(profile.Passenger.OptionalServices);
                    foreach (var optSvc in outboundOptionalServices)
                    {
                        var svc = allAdditionalServices[optSvc.AdditionalServiceId];
                        _dbContext.BookingServices.Add(new API.Domain.Entities.BookingService
                        {
                            BookingPassengerId = passenger.Id,
                            BookingLegPassengerId = outboundLegPassenger.Id,
                            AdditionalServiceId = optSvc.AdditionalServiceId,
                            Quantity = optSvc.Quantity,
                            Price = svc.Price
                        });
                    }

                    if (returnLeg != null && returnInventory != null)
                    {
                        var returnLegPassenger = new BookingLegPassenger
                        {
                            BookingLegId = returnLeg.Id,
                            BookingPassengerId = passenger.Id,
                            FlightSeatInventoryId = returnInventory.Id,
                            DocumentCheckStatus = ResolveInitialDocumentCheckStatus(profile.Type),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _dbContext.Set<BookingLegPassenger>().Add(returnLegPassenger);
                        await _dbContext.SaveChangesAsync();

                        if (profile.Type != PassengerType.Infant)
                        {
                            foreach (var includedId in includedServiceIds)
                            {
                                _dbContext.BookingServices.Add(new API.Domain.Entities.BookingService
                                {
                                    BookingPassengerId = passenger.Id,
                                    BookingLegPassengerId = returnLegPassenger.Id,
                                    AdditionalServiceId = includedId,
                                    Quantity = 1,
                                    Price = 0
                                });
                            }
                        }

                        var returnOptionalServices = NormalizeServices(profile.Passenger.ReturnOptionalServices);
                        foreach (var optSvc in returnOptionalServices)
                        {
                            var svc = allAdditionalServices[optSvc.AdditionalServiceId];
                            _dbContext.BookingServices.Add(new API.Domain.Entities.BookingService
                            {
                                BookingPassengerId = passenger.Id,
                                BookingLegPassengerId = returnLegPassenger.Id,
                                AdditionalServiceId = optSvc.AdditionalServiceId,
                                Quantity = optSvc.Quantity,
                                Price = svc.Price
                            });
                        }
                    }
                }

                // 7. Hold seats atomically within transaction for each leg
                if (seatsToHold > 0)
                {
                    var holdSucceeded = await _unitOfWork.FlightSeatInventories
                        .TryHoldSeatsAtomicAsync(outboundInventory.Id, seatsToHold);
                    if (!holdSucceeded)
                    {
                        throw new ConcurrencyException("Unable to hold seats due to concurrent updates. Please retry.");
                    }

                    if (returnInventory != null)
                    {
                        var returnHoldSucceeded = await _unitOfWork.FlightSeatInventories
                            .TryHoldSeatsAtomicAsync(returnInventory.Id, seatsToHold);
                        if (!returnHoldSucceeded)
                        {
                            throw new ConcurrencyException("Unable to hold return-leg seats due to concurrent updates. Please retry.");
                        }
                    }
                }

                _logger.LogInformation("Booking created atomically: {BookingCode}", booking.BookingCode);

                return await BuildBookingResponseAsync(createdBooking);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            throw;
        }
    }

    public async Task<bool> CancelBookingAsync(int bookingId, int userId, string reason)
    {
        try
        {
            // QUAN TRỌNG: Không dùng transaction ở đây vì cần gọi external API trước
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
            {
                throw new UnauthorizedException("Cannot cancel this booking");
            }

            if (booking.Status != (int)BookingStatus.Pending
                && booking.Status != (int)BookingStatus.Confirmed
                && booking.Status != (int)BookingStatus.PartiallyCancelled)
            {
                throw new ValidationException("Only pending, confirmed, or partially cancelled bookings can be cancelled");
            }

            await EnsureBookingChangeAllowedAsync(booking.Id);

            if (booking.Status == (int)BookingStatus.Confirmed)
            {
                var flight = await _unitOfWork.Flights.GetByIdAsync(booking.OutboundFlightId);
                var hoursToDeparture = (flight!.DepartureTime - DateTime.UtcNow).TotalHours;

                if (hoursToDeparture < 24)
                {
                    throw new ValidationException("Cannot cancel within 24 hours of departure");
                }
            }

            var previousStatus = booking.Status;

            // BƯỚC 1: Nếu booking đã confirmed (đã thanh toán) → Phải hoàn tiền TRƯỚC
            if (previousStatus == (int)BookingStatus.Confirmed)
            {
                _logger.LogInformation("Booking {BookingId} is confirmed, processing refund before cancellation", bookingId);
                
                // Tìm payment đã completed
                var payments = await _unitOfWork.Payments.GetByBookingIdAsync(bookingId);
                var completedPayment = payments.FirstOrDefault(p => p.Status == (int)PaymentStatus.Completed);
                
                if (completedPayment == null)
                {
                    throw new ValidationException("Cannot find completed payment for this booking");
                }

                // Gọi refund API ĐỒNG BỘ (không phải background job)
                var refundableAmount = await CalculateBookingRefundAmountForCancellationAsync(bookingId);
                if (refundableAmount <= 0)
                {
                    _logger.LogInformation(
                        "Booking {BookingId} has no refundable tickets (excluding checked-in/cancelled/refunded). Skip refund API call.",
                        bookingId);
                }

                var refundSuccess = refundableAmount > 0
                    ? await ProcessRefundForCancellationAsync(completedPayment, reason, refundableAmount)
                    : true;
                
                if (!refundSuccess)
                {
                    _logger.LogWarning(
                        "Refund failed for booking {BookingId}. Continue cancellation and mark payment PendingRefund for manual handling.",
                        bookingId);
                }
                
                if (refundSuccess)
                {
                    _logger.LogInformation("Refund successful for booking {BookingId}, proceeding with cancellation", bookingId);
                }
            }

            // BƯỚC 2: Refund thành công (hoặc booking chưa thanh toán) → Mới hủy booking
            var cancelled = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var trackedBooking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
                if (trackedBooking == null)
                {
                    throw new NotFoundException("Booking not found");
                }

                var ticketsToCancel = await _dbContext.Tickets
                    .Where(t => t.BookingId == bookingId && !t.IsDeleted && t.Status == 0)
                    .ToListAsync();

                foreach (var ticket in ticketsToCancel)
                {
                    var seatInventory = await _unitOfWork.FlightSeatInventories
                        .GetByFlightAndSeatClassAsync(ticket.FlightId, ticket.SeatClassId);
                    if (seatInventory == null)
                    {
                        throw new NotFoundException("Seat inventory not found for ticket");
                    }

                    var inventoryUpdateSucceeded = await TryRestoreSeatsForCancellationAsync(
                        seatInventory.Id,
                        1,
                        previousStatus);
                    if (!inventoryUpdateSucceeded)
                    {
                        throw new ConcurrencyException("Unable to update seat inventory due to concurrent updates. Please retry.");
                    }

                    ticket.Status = 3; // Cancelled by booking cancellation
                    ticket.UpdatedAt = DateTime.UtcNow;
                }

                var bookingTickets = await _dbContext.Tickets
                    .Where(t => t.BookingId == bookingId && !t.IsDeleted)
                    .ToListAsync();
                var hasActive = bookingTickets.Any(t => t.Status == 0 || t.Status == 1 || t.Status == 2);
                trackedBooking.Status = hasActive
                    ? (int)BookingStatus.PartiallyCancelled
                    : (int)BookingStatus.Cancelled;
                trackedBooking.UpdatedAt = DateTime.UtcNow;

                if (previousStatus == (int)BookingStatus.Pending
                    && trackedBooking.PromotionId.HasValue
                    && trackedBooking.DiscountAmount > 0)
                {
                    await _unitOfWork.Promotions.ReleaseUsageAsync(trackedBooking.PromotionId.Value);
                }

                _logger.LogInformation("Booking {BookingId} cancelled successfully", bookingId);

                _logger.LogInformation(
                    "Booking cancelled: {BookingId}. PreviousStatus: {PreviousStatus}. PassengerCount: {PassengerCount}",
                    bookingId,
                    previousStatus,
                    bookingTickets.Count);

                return true;
            });

            if (cancelled)
            {
                await NotifyBookingCancelledAsync(bookingId, reason);
            }

            return cancelled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking");
            throw;
        }
    }
    public async Task<bool> UpdateBookingAsync(int bookingId, int userId, UpdateBookingDto dto)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
            {
                throw new UnauthorizedException("Cannot update this booking");
            }

            if (booking.Status != (int)BookingStatus.Pending)
            {
                throw new ValidationException("Can only update pending bookings");
            }

            await EnsureBookingChangeAllowedAsync(booking.Id);

            if (dto.Passengers != null && dto.Passengers.Any())
            {
                var existingPassengers = await _unitOfWork.BookingPassengers.GetByBookingIdAsync(bookingId);
                
                foreach (var passengerDto in dto.Passengers)
                {
                    var passenger = existingPassengers.FirstOrDefault(p => p.Id == passengerDto.PassengerId);
                    if (passenger != null)
                    {
                        passenger.FullName = $"{passengerDto.FirstName} {passengerDto.LastName}".Trim();
                        await _unitOfWork.BookingPassengers.UpdateAsync(passenger);
                    }
                }
            }

            _logger.LogInformation("Booking updated: {BookingId}", bookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking");
            throw;
        }
    }

    public async Task<BookingResponse> GetBookingAsync(int bookingId, int userId)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
            {
                throw new UnauthorizedException("Cannot access this booking");
            }

            return await BuildBookingResponseAsync(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking");
            throw;
        }
    }

    public async Task<List<BookingResponse>> GetUserBookingsAsync(int userId, int page = 1, int pageSize = 10)
    {
        try
        {
            var bookings = await _unitOfWork.Bookings.GetByUserIdAsync(userId, page, pageSize);
            var responses = new List<BookingResponse>();

            foreach (var booking in bookings)
            {
                responses.Add(await BuildBookingResponseAsync(booking));
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user bookings");
            throw;
        }
    }

    public async Task<List<PassengerServiceResponse>> GetPassengerServicesAsync(int bookingId, int passengerId, int userId)
    {
        await GetAuthorizedBookingPassengerAsync(bookingId, passengerId, userId, requirePending: false);

        var includedServiceIds = await GetIncludedServiceIdsForPassengerAsync(passengerId);
        var services = await _dbContext.BookingServices
            .Include(bs => bs.AdditionalService)
            .Where(bs => bs.BookingPassengerId == passengerId && !bs.IsDeleted)
            .OrderBy(bs => bs.Id)
            .ToListAsync();

        return services.Select(bs => MapPassengerServiceResponse(bs, includedServiceIds)).ToList();
    }

    public async Task<PassengerServiceResponse> AddPassengerServiceAsync(
        int bookingId,
        int passengerId,
        int userId,
        AddPassengerServiceDto dto)
    {
        ValidatePassengerServiceQuantity(dto.Quantity);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (_, passenger) = await GetAuthorizedBookingPassengerAsync(bookingId, passengerId, userId, requirePending: true);
            if (passenger.PassengerType == (int)PassengerType.Infant)
            {
                throw new ValidationException("Infant passengers cannot use optional services");
            }

            var service = await _dbContext.AdditionalServices
                .FirstOrDefaultAsync(s => s.Id == dto.AdditionalServiceId && !s.IsDeleted);
            if (service == null)
            {
                throw new NotFoundException("Additional service not found");
            }

            if (await IsIncludedServiceForPassengerAsync(passengerId, dto.AdditionalServiceId))
            {
                throw new ValidationException("This service is already included for the passenger seat class");
            }
            if (!await IsOptionalServiceAllowedForPassengerAsync(passengerId, dto.AdditionalServiceId))
            {
                throw new ValidationException("This service is not available for the passenger seat class");
            }

            var existingService = await _dbContext.BookingServices
                .FirstOrDefaultAsync(bs =>
                    bs.BookingPassengerId == passengerId &&
                    bs.AdditionalServiceId == dto.AdditionalServiceId);

            if (existingService != null && !existingService.IsDeleted)
            {
                throw new ValidationException("Passenger already has this service");
            }

            BookingServiceEntity bookingService;
            if (existingService != null)
            {
                existingService.Restore();
                existingService.Quantity = dto.Quantity;
                existingService.Price = service.Price;
                existingService.AdditionalService = service;
                bookingService = existingService;
            }
            else
            {
                bookingService = new BookingServiceEntity
                {
                    BookingPassengerId = passengerId,
                    AdditionalServiceId = dto.AdditionalServiceId,
                    Quantity = dto.Quantity,
                    Price = service.Price,
                    AdditionalService = service
                };
                _dbContext.BookingServices.Add(bookingService);
            }

            await AdjustBookingAmountAsync(bookingId, service.Price * dto.Quantity);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Added service {AdditionalServiceId} to passenger {PassengerId} in booking {BookingId}",
                dto.AdditionalServiceId,
                passengerId,
                bookingId);

            return MapPassengerServiceResponse(bookingService, new HashSet<int>());
        });
    }

    public async Task<PassengerServiceResponse> UpdatePassengerServiceAsync(
        int bookingId,
        int passengerId,
        int bookingServiceId,
        int userId,
        UpdatePassengerServiceDto dto)
    {
        ValidatePassengerServiceQuantity(dto.Quantity);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await GetAuthorizedBookingPassengerAsync(bookingId, passengerId, userId, requirePending: true);

            var bookingService = await _dbContext.BookingServices
                .Include(bs => bs.AdditionalService)
                .FirstOrDefaultAsync(bs =>
                    bs.Id == bookingServiceId &&
                    bs.BookingPassengerId == passengerId &&
                    !bs.IsDeleted);

            if (bookingService == null)
            {
                throw new NotFoundException("Passenger service not found");
            }

            var includedServiceIds = await GetIncludedServiceIdsForPassengerAsync(passengerId);
            if (includedServiceIds.Contains(bookingService.AdditionalServiceId))
            {
                throw new ValidationException("Included services cannot be updated");
            }

            var oldTotal = bookingService.Price * bookingService.Quantity;
            bookingService.Quantity = dto.Quantity;
            var newTotal = bookingService.Price * bookingService.Quantity;
            await AdjustBookingAmountAsync(bookingId, newTotal - oldTotal);

            _logger.LogInformation(
                "Updated service {BookingServiceId} for passenger {PassengerId} in booking {BookingId}",
                bookingServiceId,
                passengerId,
                bookingId);

            return MapPassengerServiceResponse(bookingService, includedServiceIds);
        });
    }

    public async Task<bool> RemovePassengerServiceAsync(int bookingId, int passengerId, int bookingServiceId, int userId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await GetAuthorizedBookingPassengerAsync(bookingId, passengerId, userId, requirePending: true);

            var bookingService = await _dbContext.BookingServices
                .FirstOrDefaultAsync(bs =>
                    bs.Id == bookingServiceId &&
                    bs.BookingPassengerId == passengerId &&
                    !bs.IsDeleted);

            if (bookingService == null)
            {
                throw new NotFoundException("Passenger service not found");
            }

            var includedServiceIds = await GetIncludedServiceIdsForPassengerAsync(passengerId);
            if (includedServiceIds.Contains(bookingService.AdditionalServiceId))
            {
                throw new ValidationException("Included services cannot be removed");
            }

            bookingService.SoftDelete();
            await AdjustBookingAmountAsync(bookingId, -(bookingService.Price * bookingService.Quantity));

            _logger.LogInformation(
                "Removed service {BookingServiceId} from passenger {PassengerId} in booking {BookingId}",
                bookingServiceId,
                passengerId,
                bookingId);

            return true;
        });
    }

    private async Task<BookingResponse> BuildBookingResponseAsync(Booking booking)
    {
        var outboundFlight = await _unitOfWork.Flights.GetByIdAsync(booking.OutboundFlightId);
        var outboundLeg = await _dbContext.BookingLegs
            .FirstOrDefaultAsync(l => !l.IsDeleted && l.BookingId == booking.Id && l.LegType == 0);
        var returnLeg = await _dbContext.BookingLegs
            .FirstOrDefaultAsync(l => !l.IsDeleted && l.BookingId == booking.Id && l.LegType == 1);
        var seatClassMap = await _dbContext.SeatClasses
            .Where(sc => !sc.IsDeleted)
            .ToDictionaryAsync(sc => sc.Id, sc => sc.Name);
        var outboundPriceFromTickets = await _dbContext.Tickets
            .Where(t => !t.IsDeleted && t.BookingId == booking.Id && t.FlightId == booking.OutboundFlightId)
            .SumAsync(t => (decimal?)t.Price) ?? 0m;
        var returnPriceFromTickets = booking.ReturnFlightId.HasValue
            ? await _dbContext.Tickets
                .Where(t => !t.IsDeleted && t.BookingId == booking.Id && t.FlightId == booking.ReturnFlightId.Value)
                .SumAsync(t => (decimal?)t.Price) ?? 0m
            : 0m;
        var passengers = await _unitOfWork.BookingPassengers.GetByBookingIdAsync(booking.Id);
        var passengerIds = passengers.Select(p => p.Id).ToList();
        
        var bookingServices = await _dbContext.BookingServices
            .Include(bs => bs.AdditionalService)
            .Where(bs => passengerIds.Contains(bs.BookingPassengerId) && !bs.IsDeleted)
            .ToListAsync();

        var statusString = booking.Status switch
        {
            (int)BookingStatus.Pending => "Pending",
            (int)BookingStatus.Confirmed => "Confirmed",
            (int)BookingStatus.CheckedIn => "CheckedIn",
            (int)BookingStatus.Cancelled => "Cancelled",
            (int)BookingStatus.Refunded => "Refunded",
            (int)BookingStatus.PartiallyCancelled => "PartiallyCancelled",
            (int)BookingStatus.PendingDisruptionDecision => "PendingDisruptionDecision",
            _ => "Unknown"
        };

        var response = new BookingResponse
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            Status = statusString,
            TotalAmount = booking.TotalAmount,
            PromotionId = booking.PromotionId,
            FinalAmount = booking.FinalAmount,
            DiscountAmount = booking.DiscountAmount,
            CreatedAt = VietnamTime.ToVietnamTime(booking.CreatedAt),
            ExpiresAt = booking.ExpiresAt.HasValue ? VietnamTime.ToVietnamTime(booking.ExpiresAt.Value) : null,
            OutboundFlight = new FlightBookingDetail
            {
                FlightId = outboundFlight!.Id,
                FlightNumber = outboundFlight.FlightNumber,
                DepartureAirport = outboundFlight.Route.DepartureAirport.Code,
                ArrivalAirport = outboundFlight.Route.ArrivalAirport.Code,
                DepartureTime = VietnamTime.ToVietnamTime(outboundFlight.DepartureTime),
                ArrivalTime = VietnamTime.ToVietnamTime(outboundFlight.ArrivalTime),
                SeatClass = outboundLeg != null && seatClassMap.TryGetValue(outboundLeg.SeatClassId, out var outboundSeatClassName)
                    ? outboundSeatClassName
                    : "Unknown",
                Price = outboundPriceFromTickets
            },
            Passengers = passengers.Select(p => new PassengerDetail
            {
                PassengerId = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                PassportNumber = p.PassportNumber ?? "",
                Status = "Confirmed",
                DocumentCheckStatus = ((PassengerDocumentCheckStatus)p.DocumentCheckStatus).ToString(),
                Services = bookingServices
                    .Where(bs => bs.BookingPassengerId == p.Id)
                    .Select(bs => new BookingServiceDetail
                    {
                        AdditionalServiceId = bs.AdditionalServiceId,
                        ServiceName = bs.AdditionalService?.ServiceName ?? "",
                        Quantity = bs.Quantity,
                        Price = bs.Price
                    }).ToList()
            }).ToList()
        };

        if (booking.ReturnFlightId.HasValue)
        {
            var returnFlight = await _unitOfWork.Flights.GetByIdAsync(booking.ReturnFlightId.Value);
            response.ReturnFlight = new FlightBookingDetail
            {
                FlightId = returnFlight!.Id,
                FlightNumber = returnFlight.FlightNumber,
                DepartureAirport = returnFlight.Route.DepartureAirport.Code,
                ArrivalAirport = returnFlight.Route.ArrivalAirport.Code,
                DepartureTime = VietnamTime.ToVietnamTime(returnFlight.DepartureTime),
                ArrivalTime = VietnamTime.ToVietnamTime(returnFlight.ArrivalTime),
                SeatClass = returnLeg != null && seatClassMap.TryGetValue(returnLeg.SeatClassId, out var returnSeatClassName)
                    ? returnSeatClassName
                    : "Unknown",
                Price = returnPriceFromTickets
            };
        }

        return response;
    }

    private static void ValidatePassengerServiceQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("Service quantity must be greater than 0");
        }
    }

    private static PassengerType ResolvePassengerType(DateTime birthDate, DateTime departureTime)
    {
        var years = CalculateAgeAtDeparture(birthDate, departureTime);

        if (years < 2)
        {
            return PassengerType.Infant;
        }

        if (years < 12)
        {
            return PassengerType.Child;
        }

        return PassengerType.Adult;
    }

    private static int CalculateAgeAtDeparture(DateTime birthDate, DateTime departureTime)
    {
        var departureDate = VietnamTime.ToVietnamTime(departureTime).Date;
        var years = departureDate.Year - birthDate.Year;
        if (birthDate.Date > departureDate.AddYears(-years))
        {
            years--;
        }

        return years;
    }

    private static int ResolveInitialDocumentCheckStatus(PassengerType passengerType)
    {
        return passengerType == PassengerType.Infant
            ? (int)PassengerDocumentCheckStatus.NotRequired
            : (int)PassengerDocumentCheckStatus.Pending;
    }

    private async Task<(Booking Booking, BookingPassenger Passenger)> GetAuthorizedBookingPassengerAsync(
        int bookingId,
        int passengerId,
        int userId,
        bool requirePending)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found");
        }

        if (booking.UserId != userId)
        {
            throw new UnauthorizedException("Cannot access this booking");
        }

        if (requirePending && booking.Status != (int)BookingStatus.Pending)
        {
            throw new ValidationException("Can only modify services for pending bookings");
        }
        if (requirePending)
        {
            await EnsureBookingChangeAllowedAsync(bookingId);
        }

        var passenger = await _unitOfWork.BookingPassengers.GetByIdAsync(passengerId);
        if (passenger == null || passenger.BookingId != bookingId)
        {
            throw new NotFoundException("Passenger not found in this booking");
        }

        return (booking, passenger);
    }

    private async Task<HashSet<int>> GetIncludedServiceIdsForPassengerAsync(int passengerId)
    {
        var passenger = await _dbContext.BookingPassengers
            .Include(p => p.FlightSeatInventory)
            .FirstOrDefaultAsync(p => p.Id == passengerId);

        if (passenger == null)
        {
            throw new NotFoundException("Passenger not found");
        }

        return await _dbContext.ClassServiceConfigs
            .Where(c => c.SeatClassId == passenger.FlightSeatInventory.SeatClassId && c.IsIncluded)
            .Select(c => c.AdditionalServiceId)
            .ToHashSetAsync();
    }

    private async Task<bool> IsIncludedServiceForPassengerAsync(int passengerId, int additionalServiceId)
    {
        var includedServiceIds = await GetIncludedServiceIdsForPassengerAsync(passengerId);
        return includedServiceIds.Contains(additionalServiceId);
    }

    private async Task<bool> IsOptionalServiceAllowedForPassengerAsync(int passengerId, int additionalServiceId)
    {
        var passenger = await _dbContext.BookingPassengers
            .Include(p => p.FlightSeatInventory)
            .FirstOrDefaultAsync(p => p.Id == passengerId);

        if (passenger == null)
        {
            throw new NotFoundException("Passenger not found");
        }

        return await _dbContext.ClassServiceConfigs
            .AnyAsync(c =>
                c.SeatClassId == passenger.FlightSeatInventory.SeatClassId &&
                c.AdditionalServiceId == additionalServiceId &&
                !c.IsIncluded);
    }

    private async Task AdjustBookingAmountAsync(int bookingId, decimal delta)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found");
        }

        booking.TotalAmount += delta;
        booking.FinalAmount = booking.TotalAmount - booking.DiscountAmount;
        if (booking.FinalAmount < 0)
        {
            throw new ValidationException("Booking final amount cannot be negative");
        }

        booking.UpdatedAt = DateTime.UtcNow;
    }

    private static PassengerServiceResponse MapPassengerServiceResponse(
        BookingServiceEntity bookingService,
        HashSet<int> includedServiceIds)
    {
        return new PassengerServiceResponse
        {
            BookingServiceId = bookingService.Id,
            BookingPassengerId = bookingService.BookingPassengerId,
            AdditionalServiceId = bookingService.AdditionalServiceId,
            ServiceName = bookingService.AdditionalService?.ServiceName ?? "",
            Quantity = bookingService.Quantity,
            UnitPrice = bookingService.Price,
            TotalPrice = bookingService.Price * bookingService.Quantity,
            IsIncluded = includedServiceIds.Contains(bookingService.AdditionalServiceId)
        };
    }

    private static string BuildFareSnapshot(
        int outboundFlightId,
        int? returnFlightId,
        PassengerType passengerType,
        decimal outboundSeatPrice,
        decimal returnSeatPrice)
    {
        var outboundFare = passengerType switch
        {
            PassengerType.Infant => 0m,
            PassengerType.Child => outboundSeatPrice * ChildFareRate,
            _ => outboundSeatPrice
        };

        var returnFare = passengerType switch
        {
            PassengerType.Infant => 0m,
            PassengerType.Child => returnSeatPrice * ChildFareRate,
            _ => returnSeatPrice
        };

        var legs = new List<object>
        {
            new { flightId = outboundFlightId, fare = outboundFare }
        };

        if (returnFlightId.HasValue)
        {
            legs.Add(new { flightId = returnFlightId.Value, fare = returnFare });
        }

        return JsonSerializer.Serialize(new
        {
            version = 1,
            legs
        });
    }

    private async Task<decimal> CalculateLegFareAmountAsync(int bookingId, int flightId, List<Ticket> movableTickets)
    {
        var oldFareByPassenger = await GetFareByPassengerFromSnapshotAsync(bookingId, flightId, movableTickets);
        if (oldFareByPassenger.Count == movableTickets.Count)
        {
            return oldFareByPassenger.Values.Sum();
        }

        return movableTickets.Sum(t => t.Price);
    }

    private async Task<decimal> CalculateLegNewFareAmountAsync(decimal dynamicNewUnitFare, List<Ticket> movableTickets)
    {
        if (movableTickets.Count == 0)
        {
            return 0m;
        }

        var total = 0m;
        foreach (var ticket in movableTickets)
        {
            var passengerType = await GetPassengerTypeAsync(ticket.BookingPassengerId);
            total += ApplyPassengerTypeFare(dynamicNewUnitFare, passengerType);
        }

        return total;
    }

    private async Task<Dictionary<int, decimal>> GetFareByPassengerFromSnapshotAsync(int bookingId, int flightId, List<Ticket> tickets)
    {
        var passengerIds = tickets.Select(t => t.BookingPassengerId).Distinct().ToList();
        if (passengerIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var passengers = await _dbContext.BookingPassengers
            .Where(p => p.BookingId == bookingId && passengerIds.Contains(p.Id))
            .ToListAsync();

        var result = new Dictionary<int, decimal>();
        foreach (var passenger in passengers)
        {
            var fare = TryReadFareFromSnapshot(passenger.FareSnapshot, flightId);
            if (fare.HasValue)
            {
                result[passenger.Id] = fare.Value;
            }
        }

        return result;
    }

    private async Task<PassengerType> GetPassengerTypeAsync(int bookingPassengerId)
    {
        var passengerTypeValue = await _dbContext.BookingPassengers
            .Where(p => p.Id == bookingPassengerId)
            .Select(p => p.PassengerType)
            .FirstOrDefaultAsync();
        return (PassengerType)passengerTypeValue;
    }

    private static decimal ApplyPassengerTypeFare(decimal adultFare, PassengerType passengerType)
    {
        return passengerType switch
        {
            PassengerType.Infant => 0m,
            PassengerType.Child => adultFare * ChildFareRate,
            _ => adultFare
        };
    }

    private static decimal? TryReadFareFromSnapshot(string? fareSnapshot, int flightId)
    {
        if (string.IsNullOrWhiteSpace(fareSnapshot))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(fareSnapshot);
            if (!doc.RootElement.TryGetProperty("legs", out var legs) || legs.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var leg in legs.EnumerateArray())
            {
                if (!leg.TryGetProperty("flightId", out var fId) || fId.GetInt32() != flightId)
                {
                    continue;
                }

                if (leg.TryGetProperty("fare", out var fare) && fare.TryGetDecimal(out var amount))
                {
                    return amount;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private async Task<decimal> CalculateTicketRefundAmountAsync(int bookingId, Ticket ticket)
    {
        decimal serviceAmount = 0;
        var legPassengers = await _dbContext.BookingLegPassengers
            .Where(lp => lp.BookingPassengerId == ticket.PassengerId)
            .Select(lp => lp.Id)
            .ToListAsync();

        var servicesByLeg = await _dbContext.BookingServices
            .Where(bs => !bs.IsDeleted
                && bs.BookingPassengerId == ticket.PassengerId
                && bs.BookingLegPassengerId.HasValue
                && legPassengers.Contains(bs.BookingLegPassengerId.Value))
            .ToListAsync();
        if (servicesByLeg.Count > 0)
        {
            serviceAmount = servicesByLeg.Sum(s => s.Price * s.Quantity);
        }
        else
        {
            _logger.LogWarning(
                "Fallback refund service calculation without leg linkage for booking {BookingId}, ticket {TicketId}",
                bookingId,
                ticket.Id);
            serviceAmount = await _dbContext.BookingServices
                .Where(bs => !bs.IsDeleted
                    && bs.BookingPassengerId == ticket.PassengerId
                    && !bs.BookingLegPassengerId.HasValue)
                .SumAsync(bs => bs.Price * bs.Quantity);
        }

        return Math.Max(0, ticket.Price + serviceAmount);
    }

    private async Task ProcessTicketRefundAfterCommitAsync(
        int bookingId,
        int ticketId,
        int paymentId,
        int refundRequestId,
        decimal refundAmount,
        string reason)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null)
        {
            return;
        }

        var refund = await _unitOfWork.RefundRequests.GetByIdAsync(refundRequestId);
        if (refund == null || refund.Status != 0)
        {
            return;
        }

        var success = false;
        try
        {
            var provider = (payment.Provider ?? string.Empty).Trim().ToUpperInvariant();
            if (provider == "VNPAY" && !string.IsNullOrWhiteSpace(payment.TransactionRef))
            {
                var refundResponse = await _vnpayPaymentProvider.ProcessRefundAsync(new VnpayRefundRequest
                {
                    TxnRef = payment.TransactionRef,
                    TransactionDate = payment.PaidAt?.ToString("yyyyMMddHHmmss") ?? payment.CreatedAt.ToString("yyyyMMddHHmmss"),
                    Amount = refundAmount,
                    OrderInfo = $"Ticket cancel booking {bookingId}, ticket {ticketId}: {reason}".Trim(),
                    CreateBy = "SYSTEM"
                });

                success = refundResponse.Success;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refund provider failed for ticket {TicketId}", ticketId);
        }

        if (success)
        {
            refund.Status = 2;
            refund.ProcessedAt = DateTime.UtcNow;
            payment.Status = await ResolvePaymentRefundStatusAsync(payment.Id, payment.Amount, includeCurrentRefundAmount: refundAmount);
        }
        else
        {
            payment.Status = (int)PaymentStatus.PendingRefund;
        }

        payment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Payments.UpdateAsync(payment);
        await _unitOfWork.RefundRequests.UpdateAsync(refund);
    }

    private enum CancellationActor
    {
        User = 0,
        Admin = 1
    }

    private sealed record TicketCancelPostCommitContext(
        bool Changed,
        int? PaymentId,
        int? RefundRequestId,
        decimal? RefundAmount,
        int BookingId,
        int TicketId);

    public async Task<List<FlightDisruptionOptionResponse>> GetFlightDisruptionOptionsAsync(
        int bookingId,
        int userId,
        DateOnly? departureDate = null)
    {
        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found");
        }

        if (booking.UserId != userId)
        {
            throw new UnauthorizedException("Cannot access this booking");
        }

        if (booking.Status == (int)BookingStatus.PendingDisruptionDecision)
        {
            await EnsurePendingDisruptionDecisionsAsync(booking);
        }

        var decisions = await _dbContext.FlightDisruptionDecisions
            .Where(d => !d.IsDeleted && d.BookingId == bookingId && d.UserId == userId && d.Status == 0)
            .OrderBy(d => d.Id)
            .ToListAsync();

        // Self-heal inconsistent status: booking still marked pending but there is no pending decision.
        if (decisions.Count == 0 && booking.Status == (int)BookingStatus.PendingDisruptionDecision)
        {
            await CompleteDisruptionDecisionAsync(booking);
            await _dbContext.SaveChangesAsync();
        }

        var responses = new List<FlightDisruptionOptionResponse>();
        foreach (var decision in decisions)
        {
            var affectedFlight = await _unitOfWork.Flights.GetByIdAsync(decision.AffectedFlightId);
            if (affectedFlight == null)
            {
                continue;
            }

            var routeId = affectedFlight.RouteId;
            var lowerBound = departureDate.HasValue
                ? VietnamTime.VietnamDayStartToUtc(departureDate.Value.ToDateTime(TimeOnly.MinValue))
                : DateTime.UtcNow;
            var upperBound = departureDate.HasValue
                ? VietnamTime.VietnamDayEndExclusiveToUtc(departureDate.Value.ToDateTime(TimeOnly.MinValue))
                : DateTime.UtcNow.AddDays(7);
            var seatsNeeded = await CountNonInfantPassengersAsync(bookingId);

            var candidates = (await _unitOfWork.Flights.GetAllAsync())
                .Where(f => !f.IsDeleted
                    && f.Status == 0
                    && f.RouteId == routeId
                    && f.Id != decision.AffectedFlightId
                    && f.DepartureTime > lowerBound
                    && f.DepartureTime <= upperBound)
                .OrderBy(f => f.DepartureTime)
                .ToList();

            var options = new List<DisruptionFlightOptionDto>();
            var leg = await _dbContext.BookingLegs.FirstOrDefaultAsync(l =>
                !l.IsDeleted
                && l.BookingId == bookingId
                && l.FlightId == decision.AffectedFlightId
                && l.LegType == decision.LegType);
            var seatClassId = leg?.SeatClassId ?? 1;

            foreach (var candidate in candidates)
            {
                var inventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(candidate.Id, seatClassId);
                var available = inventory?.AvailableSeats ?? 0;
                if (available < seatsNeeded)
                {
                    continue;
                }

                options.Add(new DisruptionFlightOptionDto
                {
                    FlightId = candidate.Id,
                    FlightNumber = candidate.FlightNumber,
                    DepartureTime = candidate.DepartureTime,
                    ArrivalTime = candidate.ArrivalTime,
                    AvailableSeats = available
                });

                if (options.Count >= 200)
                {
                    break;
                }
            }

            responses.Add(new FlightDisruptionOptionResponse
            {
                DecisionId = decision.Id,
                BookingId = bookingId,
                AffectedFlightId = decision.AffectedFlightId,
                LegType = decision.LegType,
                DecisionDeadline = decision.DecisionDeadline,
                Status = "PENDING",
                FlightOptions = options
            });
        }

        return responses;
    }

    private async Task EnsurePendingDisruptionDecisionsAsync(Booking booking)
    {
        var affectedFlights = new List<(int FlightId, int LegType)>();

        var outboundFlight = await _unitOfWork.Flights.GetByIdAsync(booking.OutboundFlightId);
        if (outboundFlight is { IsDeleted: false, Status: 1 })
        {
            affectedFlights.Add((outboundFlight.Id, 0));
        }

        if (booking.ReturnFlightId.HasValue)
        {
            var returnFlight = await _unitOfWork.Flights.GetByIdAsync(booking.ReturnFlightId.Value);
            if (returnFlight is { IsDeleted: false, Status: 1 })
            {
                affectedFlights.Add((returnFlight.Id, 1));
            }
        }

        foreach (var affectedFlight in affectedFlights.Distinct())
        {
            var existing = await _dbContext.FlightDisruptionDecisions
                .FirstOrDefaultAsync(d =>
                    !d.IsDeleted
                    && d.BookingId == booking.Id
                    && d.AffectedFlightId == affectedFlight.FlightId
                    && d.LegType == affectedFlight.LegType);

            if (existing == null)
            {
                _dbContext.FlightDisruptionDecisions.Add(new FlightDisruptionDecision
                {
                    BookingId = booking.Id,
                    UserId = booking.UserId,
                    AffectedFlightId = affectedFlight.FlightId,
                    LegType = affectedFlight.LegType,
                    Status = 0,
                    DecisionType = 0,
                    DecisionDeadline = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Reason = "Flight cancellation requires customer decision"
                });
                continue;
            }

            if (existing.Status == 2 && existing.DecisionType == 0)
            {
                existing.Status = 0;
                existing.DecisionDeadline = DateTime.UtcNow.AddHours(24);
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> ChooseDisruptionCancelAsync(int bookingId, int decisionId, int userId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            if (booking.UserId != userId)
            {
                throw new UnauthorizedException("Cannot access this booking");
            }

            var decision = await _dbContext.FlightDisruptionDecisions
                .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == decisionId && d.BookingId == bookingId && d.UserId == userId);
            if (decision == null || decision.Status != 0)
            {
                throw new ValidationException("Decision is not pending");
            }

            var tickets = await _dbContext.Tickets
                .Where(t => !t.IsDeleted && t.BookingId == bookingId)
                .ToListAsync();

            var refundableAmount = tickets
                .Where(t => t.Status == 0 && t.CheckInTime == null)
                .Sum(t => t.Price);

            foreach (var ticket in tickets.Where(t => t.Status == 0))
            {
                ticket.Status = 4;
                ticket.UpdatedAt = DateTime.UtcNow;

                var inventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(ticket.FlightId, ticket.SeatClassId);
                if (inventory != null)
                {
                    await TryRestoreSeatsForCancellationAsync(inventory.Id, 1, booking.Status);
                }
            }

            var completedPayment = await _dbContext.Payments
                .Where(p => !p.IsDeleted && p.BookingId == bookingId && p.Status == (int)PaymentStatus.Completed)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (completedPayment != null)
            {
                if (refundableAmount > 0)
                {
                    _ = await ProcessRefundForCancellationAsync(
                        completedPayment,
                        "Customer chose cancellation after flight disruption",
                        refundableAmount);
                }
            }

            decision.Status = 1;
            decision.DecisionType = 1;
            decision.DecidedAt = DateTime.UtcNow;
            decision.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await CompleteDisruptionDecisionAsync(booking);

            if (_notificationService != null)
            {
                await _notificationService.SendNotificationAsync(
                    booking.UserId,
                    "Booking cancelled after flight disruption",
                    $"Booking {booking.BookingCode} has been cancelled as requested. Eligible refunds are being processed.",
                    type: "IN_APP",
                    category: "FLIGHT_DISRUPTION",
                    relatedEntityType: "Booking",
                    relatedEntityId: booking.Id,
                    sendEmail: true);
            }
            return true;
        });
    }

    public async Task<bool> ChooseDisruptionRebookAsync(int bookingId, int userId, RebookDisruptionDecisionDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            if (booking.UserId != userId)
            {
                throw new UnauthorizedException("Cannot access this booking");
            }

            var decision = await _dbContext.FlightDisruptionDecisions
                .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == dto.DecisionId && d.BookingId == bookingId && d.UserId == userId);
            if (decision == null || decision.Status != 0)
            {
                throw new ValidationException("Decision is not pending");
            }

            var oldFlight = await _unitOfWork.Flights.GetByIdAsync(decision.AffectedFlightId);
            var newFlight = await _unitOfWork.Flights.GetByIdAsync(dto.NewFlightId);
            if (oldFlight == null || newFlight == null || newFlight.IsDeleted || newFlight.Status != 0)
            {
                throw new ValidationException("New flight is invalid");
            }

            if (newFlight.RouteId != oldFlight.RouteId || newFlight.DepartureTime <= DateTime.UtcNow)
            {
                throw new ValidationException("New flight is not eligible for rebooking");
            }

            var leg = await _dbContext.BookingLegs.FirstOrDefaultAsync(l =>
                !l.IsDeleted
                && l.BookingId == bookingId
                && l.FlightId == decision.AffectedFlightId
                && l.LegType == decision.LegType);
            if (leg == null)
            {
                throw new ValidationException("Affected leg not found");
            }

            var movableTickets = await _dbContext.Tickets
                .Where(t => !t.IsDeleted && t.BookingId == bookingId && t.FlightId == decision.AffectedFlightId)
                .ToListAsync();
            if (movableTickets.Any(t => t.Status == 1))
            {
                throw new ValidationException("Checked-in tickets cannot be rebooked");
            }

            var seatCount = movableTickets.Count(t => t.Status == 0);
            var newInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(dto.NewFlightId, leg.SeatClassId);
            if (newInventory == null || newInventory.AvailableSeats < seatCount)
            {
                throw new ValidationException("Insufficient seats on selected flight");
            }

            var oldInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(decision.AffectedFlightId, leg.SeatClassId);
            if (oldInventory != null && seatCount > 0)
            {
                await TryRestoreSeatsForCancellationAsync(oldInventory.Id, seatCount, booking.Status);
            }

            if (seatCount > 0)
            {
                if (booking.Status == (int)BookingStatus.Pending)
                {
                    newInventory.HoldSeats(seatCount);
                }
                else
                {
                    newInventory.HoldSeats(seatCount);
                    newInventory.ConfirmHeldSeats(seatCount);
                }
                await _unitOfWork.FlightSeatInventories.UpdateAsync(newInventory);
            }

            leg.FlightId = dto.NewFlightId;
            leg.UpdatedAt = DateTime.UtcNow;
            if (decision.LegType == 0)
            {
                booking.OutboundFlightId = dto.NewFlightId;
            }
            else
            {
                booking.ReturnFlightId = dto.NewFlightId;
            }

            foreach (var ticket in movableTickets.Where(t => t.Status == 0))
            {
                ticket.FlightId = dto.NewFlightId;
                ticket.UpdatedAt = DateTime.UtcNow;
            }

            decision.Status = 1;
            decision.DecisionType = 2;
            decision.NewFlightId = dto.NewFlightId;
            decision.DecidedAt = DateTime.UtcNow;
            decision.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await CompleteDisruptionDecisionAsync(booking);

            if (_notificationService != null)
            {
                await _notificationService.SendNotificationAsync(
                    booking.UserId,
                    "Flight rebooked successfully",
                    $"Booking {booking.BookingCode} has been rebooked to flight {newFlight.FlightNumber}.",
                    type: "IN_APP",
                    category: "FLIGHT_DISRUPTION",
                    relatedEntityType: "Booking",
                    relatedEntityId: booking.Id,
                    sendEmail: true);
            }
            return true;
        });
    }

    public async Task<ChangeFlightOptionResponse> GetChangeFlightOptionsAsync(int bookingId, int userId, int legType, DateOnly? departureDate = null)
    {
        if (legType != 0 && legType != 1)
        {
            throw new ValidationException("LegType must be 0 (Outbound) or 1 (Return)");
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found");
        }

        if (booking.UserId != userId)
        {
            throw new UnauthorizedException("Cannot access this booking");
        }

        if (booking.Status != (int)BookingStatus.Confirmed && booking.Status != (int)BookingStatus.PartiallyCancelled)
        {
            throw new ValidationException("Booking status does not allow flight change");
        }

        await EnsureBookingChangeAllowedAsync(booking.Id);

        var leg = await _dbContext.BookingLegs.FirstOrDefaultAsync(l =>
            !l.IsDeleted && l.BookingId == bookingId && l.LegType == legType);
        if (leg == null)
        {
            throw new ValidationException("Selected leg was not found in booking");
        }

        var currentFlight = await _unitOfWork.Flights.GetByIdAsync(leg.FlightId);
        if (currentFlight == null || currentFlight.IsDeleted)
        {
            throw new ValidationException("Current leg flight is invalid");
        }

        var cutoffHours = Math.Max(1, _configuration.GetValue("ChangeFlight:CutoffHours", 3));
        if (currentFlight.DepartureTime <= DateTime.UtcNow.AddHours(cutoffHours))
        {
            throw new ValidationException("Cannot change flight within cutoff time");
        }

        var movableTickets = await _dbContext.Tickets
            .Where(t => !t.IsDeleted && t.BookingId == bookingId && t.FlightId == leg.FlightId && t.Status == 0)
            .ToListAsync();
        if (movableTickets.Count == 0)
        {
            throw new ValidationException("No eligible issued tickets found for selected leg");
        }

        var seatsNeeded = movableTickets.Count;
        var routeId = currentFlight.RouteId;
        var lowerBound = departureDate.HasValue
            ? VietnamTime.VietnamDayStartToUtc(departureDate.Value.ToDateTime(TimeOnly.MinValue))
            : DateTime.UtcNow;
        var upperBound = departureDate.HasValue
            ? VietnamTime.VietnamDayEndExclusiveToUtc(departureDate.Value.ToDateTime(TimeOnly.MinValue))
            : DateTime.UtcNow.AddDays(7);

        var candidates = (await _unitOfWork.Flights.GetAllAsync())
            .Where(f => !f.IsDeleted
                && f.Status == 0
                && f.RouteId == routeId
                && f.Id != currentFlight.Id
                && f.DepartureTime > DateTime.UtcNow.AddHours(cutoffHours)
                && f.DepartureTime >= lowerBound
                && f.DepartureTime <= upperBound)
            .OrderBy(f => f.DepartureTime)
            .Take(200)
            .ToList();

        var options = new List<ChangeFlightCandidateDto>();
        foreach (var candidate in candidates)
        {
            var inventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(candidate.Id, leg.SeatClassId);
            var available = inventory?.AvailableSeats ?? 0;
            if (available < seatsNeeded || inventory == null)
            {
                continue;
            }

            options.Add(new ChangeFlightCandidateDto
            {
                FlightId = candidate.Id,
                FlightNumber = candidate.FlightNumber,
                DepartureTime = candidate.DepartureTime,
                ArrivalTime = candidate.ArrivalTime,
                AvailableSeats = available,
                UnitFare = await _pricingService.CalculateCurrentPriceAsync(inventory.Id)
            });
        }

        return new ChangeFlightOptionResponse
        {
            BookingId = bookingId,
            LegType = legType,
            CurrentFlightId = currentFlight.Id,
            CurrentFlightNumber = currentFlight.FlightNumber,
            CurrentDepartureTime = currentFlight.DepartureTime,
            CurrentArrivalTime = currentFlight.ArrivalTime,
            Candidates = options
        };
    }

    public async Task<ChangeFlightQuoteResponseDto> GetChangeFlightQuoteAsync(int bookingId, int userId, ChangeFlightQuoteRequestDto dto)
    {
        if (dto.LegType != 0 && dto.LegType != 1)
        {
            throw new ValidationException("LegType must be 0 (Outbound) or 1 (Return)");
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found");
        }

        if (booking.UserId != userId)
        {
            throw new UnauthorizedException("Cannot access this booking");
        }

        if (booking.Status != (int)BookingStatus.Confirmed && booking.Status != (int)BookingStatus.PartiallyCancelled)
        {
            throw new ValidationException("Booking status does not allow flight change");
        }

        await EnsureBookingChangeAllowedAsync(booking.Id);

        var leg = await _dbContext.BookingLegs.FirstOrDefaultAsync(l =>
            !l.IsDeleted && l.BookingId == bookingId && l.LegType == dto.LegType);
        if (leg == null)
        {
            throw new ValidationException("Selected leg was not found in booking");
        }

        var oldFlight = await _unitOfWork.Flights.GetByIdAsync(leg.FlightId);
        var newFlight = await _unitOfWork.Flights.GetByIdAsync(dto.NewFlightId);
        if (oldFlight == null || newFlight == null || newFlight.IsDeleted || newFlight.Status != 0)
        {
            throw new ValidationException("New flight is invalid");
        }

        var cutoffHours = Math.Max(1, _configuration.GetValue("ChangeFlight:CutoffHours", 3));
        if (oldFlight.DepartureTime <= DateTime.UtcNow.AddHours(cutoffHours))
        {
            throw new ValidationException("Cannot change flight within cutoff time");
        }

        if (newFlight.RouteId != oldFlight.RouteId || newFlight.DepartureTime <= DateTime.UtcNow.AddHours(cutoffHours))
        {
            throw new ValidationException("New flight must be on the same route and satisfy cutoff");
        }

        var movableTickets = await _dbContext.Tickets
            .Where(t => !t.IsDeleted && t.BookingId == bookingId && t.FlightId == leg.FlightId && t.Status == 0)
            .ToListAsync();
        if (movableTickets.Count == 0)
        {
            throw new ValidationException("No eligible issued tickets found for selected leg");
        }

        if (movableTickets.Any(t => t.CheckInTime != null))
        {
            throw new ValidationException("Checked-in tickets cannot be changed");
        }

        var newInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(newFlight.Id, leg.SeatClassId);
        if (newInventory == null || newInventory.AvailableSeats < movableTickets.Count)
        {
            throw new ValidationException("Insufficient seats on selected flight");
        }

        var dynamicNewUnitFare = await _pricingService.CalculateCurrentPriceAsync(newInventory.Id);
        var oldAmount = await CalculateLegFareAmountAsync(bookingId, oldFlight.Id, movableTickets);
        var newAmount = await CalculateLegNewFareAmountAsync(dynamicNewUnitFare, movableTickets);
        var fareDifference = newAmount - oldAmount;
        var changeFee = Math.Max(0m, _configuration.GetValue<decimal>("ChangeFlight:FixedFee", 100000m));
        var netAmount = fareDifference + changeFee;

        return new ChangeFlightQuoteResponseDto
        {
            BookingId = bookingId,
            LegType = dto.LegType,
            OldFlightId = oldFlight.Id,
            NewFlightId = newFlight.Id,
            OldAmount = oldAmount,
            NewAmount = newAmount,
            FareDifference = fareDifference,
            ChangeFee = changeFee,
            NetAmount = netAmount,
            Currency = booking.Currency
        };
    }

    public async Task<ConfirmChangeFlightResponseDto> ConfirmChangeFlightAsync(int bookingId, int userId, ConfirmChangeFlightRequestDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var quote = await GetChangeFlightQuoteAsync(bookingId, userId, new ChangeFlightQuoteRequestDto
            {
                LegType = dto.LegType,
                NewFlightId = dto.NewFlightId
            });

            var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            await EnsureBookingChangeAllowedAsync(booking.Id);

            var leg = await _dbContext.BookingLegs.FirstOrDefaultAsync(l =>
                !l.IsDeleted && l.BookingId == bookingId && l.LegType == dto.LegType);
            if (leg == null)
            {
                throw new ValidationException("Selected leg was not found in booking");
            }

            var movableTickets = await _dbContext.Tickets
                .Where(t => !t.IsDeleted && t.BookingId == bookingId && t.FlightId == leg.FlightId && t.Status == 0)
                .ToListAsync();
            if (movableTickets.Count == 0)
            {
                throw new ValidationException("No eligible issued tickets found for selected leg");
            }
            var oldFlightId = leg.FlightId;
            var seatsToMove = await CountSeatConsumerTicketsAsync(movableTickets);

            if (quote.NetAmount > 0)
            {
                await ExpireStaleAwaitingPaymentChangesAsync(bookingId, dto.LegType);
                var hasActiveAwaitingPayment = await _dbContext.BookingChangeRequests.AnyAsync(r =>
                    !r.IsDeleted
                    && r.BookingId == bookingId
                    && r.LegType == dto.LegType
                    && r.Status == 1);
                if (hasActiveAwaitingPayment)
                {
                    throw new ValidationException("An existing change-flight payment is in progress for this leg");
                }
            }

            var changeRequest = new BookingChangeRequest
            {
                BookingId = bookingId,
                LegType = dto.LegType,
                OldFlightId = oldFlightId,
                NewFlightId = dto.NewFlightId,
                OldAmount = quote.OldAmount,
                NewAmount = quote.NewAmount,
                ChangeFee = quote.ChangeFee,
                NetAmount = quote.NetAmount,
                CreditAmount = quote.NetAmount < 0 ? Math.Abs(quote.NetAmount) : 0m,
                Status = quote.NetAmount > 0 ? 1 : 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            _dbContext.BookingChangeRequests.Add(changeRequest);
            await _dbContext.SaveChangesAsync();

            int? paymentId = null;
            string? paymentUrl = null;
            string? qrCode = null;
            decimal creditAmount = 0m;
            if (quote.NetAmount > 0)
            {
                var newInventoryForHold = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(dto.NewFlightId, leg.SeatClassId);
                if (newInventoryForHold == null)
                {
                    throw new ValidationException("Seat inventory not found on selected flight");
                }

                var held = seatsToMove == 0
                    || await _unitOfWork.FlightSeatInventories.TryHoldSeatsAtomicAsync(newInventoryForHold.Id, seatsToMove);
                if (!held)
                {
                    throw new ValidationException("Selected flight is no longer available for change payment");
                }

                var providerResponse = await _vnpayPaymentProvider.GeneratePaymentLinkAsync(new PaymentProviderRequest
                {
                    Amount = quote.NetAmount,
                    BookingId = booking.Id,
                    Email = booking.ContactEmail,
                    OrderDescription = $"Change flight booking {booking.BookingCode}"
                });

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Provider = "VNPAY",
                    Method = "VNPAY",
                    Amount = quote.NetAmount,
                    Currency = booking.Currency,
                    Status = (int)PaymentStatus.Pending,
                    TransactionRef = providerResponse.TransactionId,
                    QrCodeData = providerResponse.QrCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                _dbContext.Payments.Add(payment);
                await _dbContext.SaveChangesAsync();

                paymentId = payment.Id;
                paymentUrl = providerResponse.PaymentLink;
                qrCode = providerResponse.QrCode ?? providerResponse.PaymentLink;
                changeRequest.PaymentId = payment.Id;
                changeRequest.Status = 1;
                changeRequest.UpdatedAt = DateTime.UtcNow;
            }
            else if (quote.NetAmount < 0)
            {
                await ApplyCompletedChangeAsync(changeRequest, booking, leg, movableTickets, quote.FareDifference, dto.NewFlightId);

                creditAmount = Math.Abs(quote.NetAmount);
                var latestBalance = await _dbContext.UserCreditLedgers
                    .Where(x => !x.IsDeleted && x.UserId == booking.UserId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => (decimal?)x.BalanceAfter)
                    .FirstOrDefaultAsync() ?? 0m;

                _dbContext.UserCreditLedgers.Add(new UserCreditLedger
                {
                    UserId = booking.UserId,
                    Amount = creditAmount,
                    BalanceAfter = latestBalance + creditAmount,
                    Currency = booking.Currency,
                    Reason = $"Flight change credit for booking {booking.BookingCode}",
                    ReferenceType = "BookingChangeRequest",
                    ReferenceId = changeRequest.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                });
            }
            else
            {
                await ApplyCompletedChangeAsync(changeRequest, booking, leg, movableTickets, quote.FareDifference, dto.NewFlightId);
            }

            await _dbContext.SaveChangesAsync();

            if (_notificationService != null)
            {
                await _notificationService.SendNotificationAsync(
                    booking.UserId,
                    "Flight change confirmed",
                    $"Booking {booking.BookingCode} has been changed to flight {dto.NewFlightId}.",
                    type: "IN_APP",
                    category: "BOOKING",
                    relatedEntityType: "BookingChangeRequest",
                    relatedEntityId: changeRequest.Id,
                    sendEmail: true);
            }

            return new ConfirmChangeFlightResponseDto
            {
                ChangeRequestId = changeRequest.Id,
                BookingId = bookingId,
                LegType = dto.LegType,
                OldFlightId = oldFlightId,
                NewFlightId = dto.NewFlightId,
                NetAmount = quote.NetAmount,
                PaymentRequired = quote.NetAmount > 0,
                PaymentId = paymentId,
                PaymentUrl = paymentUrl,
                QrCode = qrCode,
                CreditAmount = creditAmount,
                Status = quote.NetAmount > 0 ? "AWAITING_PAYMENT" : "COMPLETED"
            };
        });
    }

    private async Task CompleteDisruptionDecisionAsync(Booking booking)
    {
        var pendingCount = await _dbContext.FlightDisruptionDecisions.CountAsync(d =>
            !d.IsDeleted
            && d.BookingId == booking.Id
            && d.Status == 0);

        if (pendingCount == 0)
        {
            var anyIssued = await _dbContext.Tickets.AnyAsync(t =>
                !t.IsDeleted && t.BookingId == booking.Id && t.Status == 0);
            var anyCheckedIn = await _dbContext.Tickets.AnyAsync(t =>
                !t.IsDeleted && t.BookingId == booking.Id && t.Status == 1);

            if (anyIssued)
            {
                booking.Status = anyCheckedIn ? (int)BookingStatus.CheckedIn : (int)BookingStatus.Confirmed;
            }
            else
            {
                booking.Status = (int)BookingStatus.Cancelled;
            }
        }
        else
        {
            booking.Status = (int)BookingStatus.PendingDisruptionDecision;
        }

        booking.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<bool> IsChangeFlightPaymentAsync(int paymentId)
    {
        return await _dbContext.BookingChangeRequests.AnyAsync(r =>
            !r.IsDeleted &&
            r.PaymentId == paymentId);
    }

    public async Task<bool> ProcessChangeFlightPaymentAsync(int paymentId, string paymentStatus)
    {
        var request = await _dbContext.BookingChangeRequests
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.PaymentId == paymentId);
        if (request == null)
        {
            return false;
        }

        var isSuccess = IsSuccessfulPaymentStatus(paymentStatus);
        if (request.Status == 2)
        {
            return true;
        }

        if (request.Status != 1 && !(request.Status == 3 && isSuccess))
        {
            return false;
        }

        if (!isSuccess)
        {
            await MarkChangeRequestFailedAndReleaseHoldAsync(request);
            return false;
        }
        var requestExpired = request.CreatedAt < DateTime.UtcNow.AddMinutes(-ChangeFlightPaymentHoldMinutes);

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId && !b.IsDeleted);
        var leg = await _dbContext.BookingLegs.FirstOrDefaultAsync(l =>
            !l.IsDeleted && l.BookingId == request.BookingId && l.LegType == request.LegType);
        if (booking == null || leg == null)
        {
            request.Status = 3;
            request.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return false;
        }

        var movableTickets = await _dbContext.Tickets
            .Where(t => !t.IsDeleted && t.BookingId == request.BookingId && t.FlightId == request.OldFlightId && t.Status == 0)
            .ToListAsync();
        if (movableTickets.Count == 0 && leg.FlightId != request.OldFlightId)
        {
            movableTickets = await _dbContext.Tickets
                .Where(t => !t.IsDeleted && t.BookingId == request.BookingId && t.FlightId == leg.FlightId && t.Status == 0)
                .ToListAsync();
        }
        if (movableTickets.Count == 0)
        {
            request.Status = 3;
            request.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return false;
        }

        var seatsToMove = await CountSeatConsumerTicketsAsync(movableTickets);
        await ApplyCompletedChangeAsync(
            request,
            booking,
            leg,
            movableTickets,
            request.NewAmount - request.OldAmount,
            request.NewFlightId,
            seatsAlreadyHeld: !requestExpired,
            seatCount: seatsToMove);

        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task ApplyCompletedChangeAsync(
        BookingChangeRequest changeRequest,
        Booking booking,
        BookingLeg leg,
        List<Ticket> movableTickets,
        decimal fareDifference,
        int newFlightId,
        bool seatsAlreadyHeld = false,
        int? seatCount = null)
    {
        var seatsToMove = seatCount ?? await CountSeatConsumerTicketsAsync(movableTickets);
        var oldFlightId = leg.FlightId;
        var oldInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(oldFlightId, leg.SeatClassId);
        if (oldInventory != null && seatsToMove > 0)
        {
            var released = await TryRestoreSeatsForCancellationAsync(oldInventory.Id, seatsToMove, booking.Status);
            if (!released)
            {
                throw new ConcurrencyException("Unable to release seats on original flight");
            }
        }

        var newInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(newFlightId, leg.SeatClassId);
        if (newInventory == null)
        {
            throw new ValidationException("Seat inventory not found on selected flight");
        }

        if (!seatsAlreadyHeld)
        {
            if (booking.Status == (int)BookingStatus.Pending)
            {
                newInventory.HoldSeats(seatsToMove);
            }
            else
            {
                newInventory.HoldSeats(seatsToMove);
                newInventory.ConfirmHeldSeats(seatsToMove);
            }
            await _unitOfWork.FlightSeatInventories.UpdateAsync(newInventory);
        }
        else if (booking.Status != (int)BookingStatus.Pending && seatsToMove > 0)
        {
            var confirmed = await _unitOfWork.FlightSeatInventories
                .TryConfirmHeldSeatsAtomicAsync(newInventory.Id, seatsToMove);
            if (!confirmed)
            {
                throw new ConcurrencyException("Unable to confirm held seats on new flight");
            }
        }

        leg.FlightId = newFlightId;
        leg.UpdatedAt = DateTime.UtcNow;
        if (changeRequest.LegType == 0)
        {
            booking.OutboundFlightId = newFlightId;
        }
        else
        {
            booking.ReturnFlightId = newFlightId;
        }

        booking.TotalAmount += fareDifference;
        booking.FinalAmount += fareDifference;
        booking.UpdatedAt = DateTime.UtcNow;

        var oldFareByPassenger = await GetFareByPassengerFromSnapshotAsync(booking.Id, oldFlightId, movableTickets);
        var dynamicNewUnitFare = await _pricingService.CalculateCurrentPriceAsync(newInventory.Id);
        foreach (var ticket in movableTickets)
        {
            ticket.FlightId = newFlightId;
            var oldFare = oldFareByPassenger.GetValueOrDefault(ticket.BookingPassengerId, 0m);
            var passengerType = await GetPassengerTypeAsync(ticket.BookingPassengerId);
            var newFare = ApplyPassengerTypeFare(dynamicNewUnitFare, passengerType);
            ticket.Price = Math.Max(0m, ticket.Price + (newFare - oldFare));
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        var legPassengers = await _dbContext.BookingLegPassengers
            .Include(lp => lp.BookingLeg)
            .Where(lp => !lp.IsDeleted
                && !lp.BookingLeg.IsDeleted
                && lp.BookingLeg.BookingId == booking.Id
                && lp.BookingLeg.LegType == changeRequest.LegType)
            .ToListAsync();

        foreach (var legPassenger in legPassengers)
        {
            legPassenger.FlightSeatInventoryId = newInventory.Id;
            legPassenger.UpdatedAt = DateTime.UtcNow;
        }

        changeRequest.Status = 2;
        changeRequest.UpdatedAt = DateTime.UtcNow;
    }

    private async Task ExpireStaleAwaitingPaymentChangesAsync(int bookingId, int legType)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-ChangeFlightPaymentHoldMinutes);
        var staleRequests = await _dbContext.BookingChangeRequests
            .Where(r => !r.IsDeleted
                && r.BookingId == bookingId
                && r.LegType == legType
                && r.Status == 1
                && r.CreatedAt < cutoff)
            .ToListAsync();

        foreach (var staleRequest in staleRequests)
        {
            await MarkChangeRequestFailedAndReleaseHoldAsync(staleRequest);
        }
    }

    private async Task MarkChangeRequestFailedAndReleaseHoldAsync(BookingChangeRequest request)
    {
        if (request.Status == 2 || request.Status == 3 || request.Status == 4)
        {
            return;
        }

        var issuedTickets = await _dbContext.Tickets
            .Where(t => !t.IsDeleted
                && t.BookingId == request.BookingId
                && t.FlightId == request.OldFlightId
                && t.Status == 0)
            .ToListAsync();
        var seatCount = await CountSeatConsumerTicketsAsync(issuedTickets);

        if (seatCount > 0)
        {
            var leg = await _dbContext.BookingLegs
                .FirstOrDefaultAsync(l => !l.IsDeleted
                    && l.BookingId == request.BookingId
                    && l.LegType == request.LegType);
            if (leg != null)
            {
                var newInventory = await _unitOfWork.FlightSeatInventories
                    .GetByFlightAndSeatClassAsync(request.NewFlightId, leg.SeatClassId);
                if (newInventory != null)
                {
                    await _unitOfWork.FlightSeatInventories.TryReleaseHeldSeatsAtomicAsync(newInventory.Id, seatCount);
                }
            }
        }

        request.Status = 3;
        request.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    private async Task<int> CountSeatConsumerTicketsAsync(List<Ticket> tickets)
    {
        if (tickets.Count == 0)
        {
            return 0;
        }

        var passengerIds = tickets.Select(t => t.BookingPassengerId).Distinct().ToList();
        var passengerTypeById = await _dbContext.BookingPassengers
            .Where(p => passengerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PassengerType);

        return tickets.Count(t =>
            passengerTypeById.TryGetValue(t.BookingPassengerId, out var passengerType)
            && passengerType != (int)PassengerType.Infant);
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

    private async Task<int> CountNonInfantPassengersAsync(int bookingId)
    {
        return await _dbContext.BookingPassengers
            .CountAsync(p => p.BookingId == bookingId && p.PassengerType != (int)PassengerType.Infant);
    }

    private string GenerateBookingCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    private async Task<bool> TryRestoreSeatsForCancellationAsync(int seatInventoryId, int count, int bookingStatus)
    {
        // Prefer the bucket inferred from booking status, then fallback to the other one.
        if (bookingStatus == (int)BookingStatus.Pending)
        {
            var released = await _unitOfWork.FlightSeatInventories.TryReleaseHeldSeatsAtomicAsync(seatInventoryId, count);
            if (released)
            {
                return true;
            }

            return await _unitOfWork.FlightSeatInventories.TryCancelSoldSeatsAtomicAsync(seatInventoryId, count);
        }

        var cancelled = await _unitOfWork.FlightSeatInventories.TryCancelSoldSeatsAtomicAsync(seatInventoryId, count);
        if (cancelled)
        {
            return true;
        }

        return await _unitOfWork.FlightSeatInventories.TryReleaseHeldSeatsAtomicAsync(seatInventoryId, count);
    }

    /// <summary>
    /// Process refund synchronously before cancelling booking.
    /// Returns true if refund successful, false otherwise.
    /// NOTE: Tạm thời skip gọi VNPay API vì chưa có IP whitelist.
    /// Chỉ cập nhật status trong hệ thống.
    /// </summary>
    private async Task<decimal> CalculateBookingRefundAmountForCancellationAsync(int bookingId)
    {
        var refundableTickets = await _dbContext.Tickets
            .Where(t => t.BookingId == bookingId
                && !t.IsDeleted
                && t.Status == 0)
            .ToListAsync();

        if (refundableTickets.Count == 0)
        {
            return 0m;
        }

        return refundableTickets.Sum(t => t.Price);
    }

    private async Task<bool> ProcessRefundForCancellationAsync(Payment payment, string reason, decimal refundAmount)
    {
        try
        {
            _logger.LogInformation("Processing refund for payment {PaymentId} before cancellation", payment.Id);

            if (string.IsNullOrWhiteSpace(payment.TransactionRef))
            {
                _logger.LogWarning("Payment {PaymentId} has empty transaction ref; refund cannot be processed", payment.Id);
                return false;
            }

            var provider = (payment.Provider ?? string.Empty).Trim().ToUpperInvariant();
            var refundSucceeded = false;

            if (provider == "VNPAY")
            {
                var refundRequest = new VnpayRefundRequest
                {
                    TxnRef = payment.TransactionRef,
                    TransactionDate = payment.PaidAt?.ToString("yyyyMMddHHmmss") ?? payment.CreatedAt.ToString("yyyyMMddHHmmss"),
                    Amount = refundAmount,
                    OrderInfo = $"Cancel booking {payment.BookingId}: {reason}".Trim(),
                    CreateBy = "SYSTEM"
                };

                var refundResponse = await _vnpayPaymentProvider.ProcessRefundAsync(refundRequest);
                refundSucceeded = refundResponse.Success;

                if (!refundSucceeded)
                {
                    _logger.LogWarning(
                        "VNPAY refund failed for payment {PaymentId}. Code: {Code}, Message: {Message}",
                        payment.Id,
                        refundResponse.ResponseCode,
                        refundResponse.Message);
                }
            }
            else
            {
                _logger.LogWarning("Refund not supported for provider {Provider}", payment.Provider);
                return false;
            }

            if (!refundSucceeded)
            {
                try
                {
                    payment.Status = (int)PaymentStatus.PendingRefund;
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Payments.UpdateAsync(payment);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to mark payment {PaymentId} as PendingRefund. Continue cancellation workflow.",
                        payment.Id);
                    _dbContext.Entry(payment).State = EntityState.Unchanged;
                }
                return false;
            }

            payment.Status = (int)PaymentStatus.Refunded;
            if (refundAmount < payment.Amount)
            {
                payment.Status = (int)PaymentStatus.PartialRefunded;
            }
            payment.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Payments.UpdateAsync(payment);

            _logger.LogInformation(
                "Refund processed successfully for payment {PaymentId} via provider {Provider}",
                payment.Id,
                provider);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", payment.Id);
            _dbContext.Entry(payment).State = EntityState.Unchanged;
            return false;
        }
    }

    public Task<bool> CancelTicketAsync(int bookingId, int ticketId, int userId, string reason)
    {
        return CancelTicketInternalAsync(bookingId, ticketId, reason, actorType: CancellationActor.User, actorUserId: userId);
    }

    public Task<bool> CancelTicketByAdminAsync(int bookingId, int ticketId, int? adminUserId, string reason)
    {
        return CancelTicketInternalAsync(bookingId, ticketId, reason, actorType: CancellationActor.Admin, actorUserId: adminUserId);
    }

    private async Task<int> ResolvePaymentRefundStatusAsync(int paymentId, decimal paymentAmount, decimal includeCurrentRefundAmount)
    {
        var processedAmount = await _dbContext.RefundRequests
            .Where(r => !r.IsDeleted && r.PaymentId == paymentId && r.Status == 2)
            .SumAsync(r => (decimal?)r.RefundAmount) ?? 0m;

        var totalRefunded = processedAmount + includeCurrentRefundAmount;
        return totalRefunded >= paymentAmount
            ? (int)PaymentStatus.Refunded
            : (int)PaymentStatus.PartialRefunded;
    }

    public async Task<bool> CheckInTicketByAdminAsync(int bookingId, int ticketId, int? adminUserId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            if (booking.Status != (int)BookingStatus.Confirmed
                && booking.Status != (int)BookingStatus.CheckedIn
                && booking.Status != (int)BookingStatus.PartiallyCancelled)
            {
                throw new ValidationException("Booking status does not allow ticket check-in");
            }

            var ticket = await _dbContext.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.BookingId == bookingId && !t.IsDeleted);
            if (ticket == null)
            {
                throw new NotFoundException("Ticket not found in booking");
            }

            await EnsureTicketChangeAllowedAsync(ticket.FlightId);

            if (ticket.Status == 1)
            {
                return false;
            }

            if (ticket.Status == 2 || ticket.Status == 3 || ticket.Status == 4 || ticket.Status == 5)
            {
                throw new ValidationException("Cancelled or refunded ticket cannot be checked in");
            }

            ticket.Status = 1;
            ticket.CheckInTime = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            if (booking.Status == (int)BookingStatus.Confirmed || booking.Status == (int)BookingStatus.PartiallyCancelled)
            {
                booking.Status = (int)BookingStatus.CheckedIn;
                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = adminUserId;
            }

            return true;
        });
    }

    private async Task<bool> CancelTicketInternalAsync(
        int bookingId,
        int ticketId,
        string reason,
        CancellationActor actorType,
        int? actorUserId)
    {
        var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? "Customer request" : reason.Trim();
        var postCommit = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            if (actorType == CancellationActor.User && booking.UserId != actorUserId)
            {
                throw new UnauthorizedException("Cannot cancel this booking ticket");
            }

            var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.BookingId == bookingId && !t.IsDeleted);
            if (ticket == null)
            {
                throw new NotFoundException("Ticket not found in booking");
            }

            await EnsureTicketChangeAllowedAsync(ticket.FlightId);

            if (ticket.Status == 4 || ticket.Status == 5 || ticket.Status == 3)
            {
                return new TicketCancelPostCommitContext(false, null, null, null, bookingId, ticketId);
            }

            if (ticket.Status == 1)
            {
                throw new ValidationException("Used tickets cannot be cancelled");
            }

            if (booking.Status != (int)BookingStatus.Pending
                && booking.Status != (int)BookingStatus.Confirmed
                && booking.Status != (int)BookingStatus.PartiallyCancelled)
            {
                throw new ValidationException("Booking status does not allow ticket cancellation");
            }

            var payment = await _dbContext.Payments
                .Where(p => p.BookingId == bookingId && p.Status == (int)PaymentStatus.Completed)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            var refundAmount = await CalculateTicketRefundAmountAsync(bookingId, ticket);
            ticket.Status = actorType == CancellationActor.Admin ? 5 : 4;
            ticket.UpdatedAt = DateTime.UtcNow;

            var seatInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(ticket.FlightId, ticket.SeatClassId);
            if (seatInventory == null)
            {
                throw new NotFoundException("Seat inventory not found for ticket");
            }

            var inventoryUpdateSucceeded = await TryRestoreSeatsForCancellationAsync(
                seatInventory.Id,
                1,
                booking.Status);
            if (!inventoryUpdateSucceeded)
            {
                throw new ConcurrencyException("Unable to update seat inventory due to concurrent updates. Please retry.");
            }

            RefundRequest? createdRefundRequest = null;
            if (payment != null && refundAmount > 0)
            {
                createdRefundRequest = await _dbContext.RefundRequests.FirstOrDefaultAsync(r => r.TicketId == ticketId);
                if (createdRefundRequest == null)
                {
                    createdRefundRequest = new RefundRequest
                    {
                        BookingId = bookingId,
                        PaymentId = payment.Id,
                        TicketId = ticket.Id,
                        RefundAmount = refundAmount,
                        SourceAmountSnapshot = refundAmount,
                        Reason = sanitizedReason,
                        Status = 0,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = actorUserId
                    };
                    _dbContext.RefundRequests.Add(createdRefundRequest);
                }
            }

            var bookingTickets = await _dbContext.Tickets
                .Where(t => t.BookingId == bookingId && !t.IsDeleted)
                .ToListAsync();
            var allCancelled = bookingTickets.All(t => t.Status == 3 || t.Status == 4 || t.Status == 5);
            var hasCancelled = bookingTickets.Any(t => t.Status == 3 || t.Status == 4 || t.Status == 5);
            var hasActive = bookingTickets.Any(t => t.Status == 0 || t.Status == 1 || t.Status == 2);

            if (allCancelled)
            {
                booking.Status = (int)BookingStatus.Cancelled;
            }
            else if (hasCancelled && hasActive)
            {
                booking.Status = (int)BookingStatus.PartiallyCancelled;
            }

            booking.UpdatedAt = DateTime.UtcNow;

            return new TicketCancelPostCommitContext(
                true,
                payment?.Id,
                createdRefundRequest?.Id,
                refundAmount > 0 ? refundAmount : null,
                bookingId,
                ticketId);
        });

        if (!postCommit.Changed)
        {
            return false;
        }

        if (postCommit.PaymentId.HasValue && postCommit.RefundRequestId.HasValue && postCommit.RefundAmount.HasValue)
        {
            await ProcessTicketRefundAfterCommitAsync(
                postCommit.BookingId,
                postCommit.TicketId,
                postCommit.PaymentId.Value,
                postCommit.RefundRequestId.Value,
                postCommit.RefundAmount.Value,
                sanitizedReason);
        }

        await NotifyTicketCancelledAsync(
            postCommit.BookingId,
            postCommit.TicketId,
            sanitizedReason,
            postCommit.RefundAmount);

        return true;
    }

    private async Task EnsureBookingChangeAllowedAsync(int bookingId)
    {
        var now = DateTime.UtcNow;
        var departedLegExists = await _dbContext.BookingLegs
            .Where(l => !l.IsDeleted && l.BookingId == bookingId)
            .Join(
                _dbContext.Flights.Where(f => !f.IsDeleted),
                leg => leg.FlightId,
                flight => flight.Id,
                (leg, flight) => flight.DepartureTime)
            .AnyAsync(departureTime => departureTime <= now);

        if (departedLegExists)
        {
            throw new ValidationException("Flight has departed. Booking changes are no longer allowed.");
        }
    }

    private async Task EnsureTicketChangeAllowedAsync(int flightId)
    {
        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
        if (flight == null || flight.IsDeleted)
        {
            throw new ValidationException("Flight is invalid");
        }

        if (flight.DepartureTime <= DateTime.UtcNow)
        {
            throw new ValidationException("Flight has departed. Booking changes are no longer allowed.");
        }
    }

    private async Task NotifyBookingCancelledAsync(int bookingId, string reason)
    {
        if (_notificationService == null)
        {
            return;
        }

        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return;
            }

            await _notificationService.SendNotificationAsync(
                booking.UserId,
                "Booking cancelled",
                $"Booking {booking.BookingCode} has been cancelled. Reason: {reason}",
                type: "IN_APP",
                category: "BOOKING",
                relatedEntityType: "Booking",
                relatedEntityId: booking.Id,
                sendEmail: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send booking cancellation notification for booking {BookingId}", bookingId);
        }
    }

    private async Task NotifyTicketCancelledAsync(int bookingId, int ticketId, string reason, decimal? refundAmount)
    {
        if (_notificationService == null)
        {
            return;
        }

        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (booking == null || ticket == null)
            {
                return;
            }

            var refundMessage = refundAmount.HasValue
                ? $" Refund amount: {refundAmount.Value:0.##} {booking.Currency}."
                : string.Empty;

            await _notificationService.SendNotificationAsync(
                booking.UserId,
                "Ticket cancelled",
                $"Ticket {ticket.TicketNumber} for booking {booking.BookingCode} has been cancelled. Reason: {reason}.{refundMessage}",
                type: "IN_APP",
                category: "TICKET",
                relatedEntityType: "Ticket",
                relatedEntityId: ticket.Id,
                sendEmail: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send ticket cancellation notification for ticket {TicketId}", ticketId);
        }
    }
}
