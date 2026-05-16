                                     namespace API.Application.Services;

using API.Application.Dtos.Booking;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BookingServiceEntity = API.Domain.Entities.BookingService;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPromotionService _promotionService;
    private readonly ILogger<BookingService> _logger;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly FlightBookingDbContext _dbContext;
    private readonly VnpayPaymentProvider _vnpayPaymentProvider;

    public BookingService(
        IUnitOfWork unitOfWork,
        IPromotionService promotionService,
        IBackgroundJobService backgroundJobService,
        FlightBookingDbContext dbContext,
        VnpayPaymentProvider vnpayPaymentProvider,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
        _backgroundJobService = backgroundJobService ?? throw new ArgumentNullException(nameof(backgroundJobService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _vnpayPaymentProvider = vnpayPaymentProvider ?? throw new ArgumentNullException(nameof(vnpayPaymentProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                if (!string.IsNullOrWhiteSpace(dto.OutboundFlightNumber) && dto.OutboundDepartureDate.HasValue)
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
                else if (!string.IsNullOrWhiteSpace(dto.ReturnFlightNumber) && dto.ReturnDepartureDate.HasValue)
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

                // 3. Validate seats available
                var outboundInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(
                    outboundFlightId, dto.SeatClassId);
                if (outboundInventory == null || outboundInventory.AvailableSeats < dto.PassengerCount)
                {
                    throw new ValidationException("Insufficient seats available");
                }

                // 4. Calculate total amount with seat class pricing and additional services
                var includedServiceIds = await _dbContext.ClassServiceConfigs
                    .Where(c => c.SeatClassId == dto.SeatClassId && c.IsIncluded)
                    .Select(c => c.AdditionalServiceId)
                    .ToListAsync();

                var allAdditionalServices = await _dbContext.AdditionalServices
                    .Where(s => !s.IsDeleted)
                    .ToDictionaryAsync(s => s.Id, s => s);

                decimal additionalServicesTotal = 0;
                foreach (var passenger in dto.Passengers)
                {
                    if (passenger.OptionalServices != null)
                    {
                        foreach (var optSvc in passenger.OptionalServices)
                        {
                            if (allAdditionalServices.TryGetValue(optSvc.AdditionalServiceId, out var svc))
                            {
                                additionalServicesTotal += svc.Price * optSvc.Quantity;
                            }
                        }
                    }
                }

                var totalAmount = outboundInventory.CurrentPrice * dto.PassengerCount + additionalServicesTotal;

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
                }

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

                // 6. Create passengers and their services
                foreach (var passengerDto in dto.Passengers)
                {
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
                        PassengerType = (int)PassengerType.Adult,
                        FlightSeatInventoryId = outboundInventory.Id
                    };

                    await _unitOfWork.BookingPassengers.CreateAsync(passenger);

                    // Add included services
                    foreach (var includedId in includedServiceIds)
                    {
                        _dbContext.BookingServices.Add(new API.Domain.Entities.BookingService
                        {
                            BookingPassengerId = passenger.Id,
                            AdditionalServiceId = includedId,
                            Quantity = 1,
                            Price = 0 // Included is free
                        });
                    }

                    // Add optional services
                    if (passengerDto.OptionalServices != null)
                    {
                        foreach (var optSvc in passengerDto.OptionalServices)
                        {
                            if (allAdditionalServices.TryGetValue(optSvc.AdditionalServiceId, out var svc))
                            {
                                _dbContext.BookingServices.Add(new API.Domain.Entities.BookingService
                                {
                                    BookingPassengerId = passenger.Id,
                                    AdditionalServiceId = optSvc.AdditionalServiceId,
                                    Quantity = optSvc.Quantity,
                                    Price = svc.Price
                                });
                            }
                        }
                    }
                }

                // 7. Hold seats atomically within transaction
                var holdSucceeded = await _unitOfWork.FlightSeatInventories
                    .TryHoldSeatsAtomicAsync(outboundInventory.Id, dto.PassengerCount);
                if (!holdSucceeded)
                {
                    throw new ConcurrencyException("Unable to hold seats due to concurrent updates. Please retry.");
                }

                if (promotion != null)
                {
                    var recorded = await _promotionService.RecordPromotionUsageAsync(
                        promotion.Id,
                        createdBooking.Id,
                        userId,
                        discountAmount);

                    if (!recorded)
                    {
                        throw new ValidationException("Failed to record promotion usage");
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

            if (booking.Status != (int)BookingStatus.Pending && booking.Status != (int)BookingStatus.Confirmed)
            {
                throw new ValidationException("Only pending or confirmed bookings can be cancelled");
            }

            if (booking.Status == (int)BookingStatus.Confirmed)
            {
                var flight = await _unitOfWork.Flights.GetByIdAsync(booking.OutboundFlightId);
                var hoursToDeparture = (flight!.DepartureTime - DateTime.UtcNow).TotalHours;

                if (hoursToDeparture < 24)
                {
                    throw new ValidationException("Cannot cancel within 24 hours of departure");
                }
            }

            var passengers = await _unitOfWork.BookingPassengers.GetByBookingIdAsync(bookingId);
            if (passengers.Count == 0)
            {
                throw new ValidationException("Booking has no passengers to cancel");
            }

            var seatInventory = await _unitOfWork.FlightSeatInventories.GetByIdAsync(
                passengers.First().FlightSeatInventoryId);
            if (seatInventory == null)
            {
                throw new NotFoundException("Seat inventory not found for booking");
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
                var refundSuccess = await ProcessRefundForCancellationAsync(completedPayment, reason);
                
                if (!refundSuccess)
                {
                    _logger.LogError("Refund failed for booking {BookingId}, cancellation aborted", bookingId);
                    throw new ValidationException("Refund failed. Please contact support or try again later.");
                }
                
                _logger.LogInformation("Refund successful for booking {BookingId}, proceeding with cancellation", bookingId);
            }

            // BƯỚC 2: Refund thành công (hoặc booking chưa thanh toán) → Mới hủy booking
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var inventoryUpdateSucceeded = previousStatus == (int)BookingStatus.Pending
                    ? await _unitOfWork.FlightSeatInventories.TryReleaseHeldSeatsAtomicAsync(seatInventory.Id, passengers.Count)
                    : await _unitOfWork.FlightSeatInventories.TryCancelSoldSeatsAtomicAsync(seatInventory.Id, passengers.Count);

                if (!inventoryUpdateSucceeded)
                {
                    throw new ConcurrencyException("Unable to update seat inventory due to concurrent updates. Please retry.");
                }

                booking.Status = (int)BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.UpdateAsync(booking);

                _logger.LogInformation("Booking {BookingId} cancelled successfully", bookingId);

                _logger.LogInformation(
                    "Booking cancelled: {BookingId}. PreviousStatus: {PreviousStatus}. PassengerCount: {PassengerCount}",
                    bookingId,
                    previousStatus,
                    passengers.Count);

                return true;
            });
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
            await GetAuthorizedBookingPassengerAsync(bookingId, passengerId, userId, requirePending: true);

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
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt,
            OutboundFlight = new FlightBookingDetail
            {
                FlightId = outboundFlight!.Id,
                FlightNumber = outboundFlight.FlightNumber,
                DepartureAirport = outboundFlight.Route.DepartureAirport.Code,
                ArrivalAirport = outboundFlight.Route.ArrivalAirport.Code,
                DepartureTime = outboundFlight.DepartureTime,
                ArrivalTime = outboundFlight.ArrivalTime,
                SeatClass = "Economy",
                Price = passengers.Count > 0 ? booking.TotalAmount / passengers.Count : 0
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
                DepartureTime = returnFlight.DepartureTime,
                ArrivalTime = returnFlight.ArrivalTime,
                SeatClass = "Economy",
                Price = 0
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

    private string GenerateBookingCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    /// <summary>
    /// Process refund synchronously before cancelling booking.
    /// Returns true if refund successful, false otherwise.
    /// NOTE: Tạm thời skip gọi VNPay API vì chưa có IP whitelist.
    /// Chỉ cập nhật status trong hệ thống.
    /// </summary>
    private async Task<bool> ProcessRefundForCancellationAsync(Payment payment, string reason)
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
                    Amount = payment.Amount,
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
                return false;
            }

            payment.Status = (int)PaymentStatus.Refunded;
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
            return false;
        }
    }
}
