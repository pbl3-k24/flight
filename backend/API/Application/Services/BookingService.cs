                                     namespace API.Application.Services;

using API.Application.Dtos.Booking;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingService> _logger;
    private readonly IBackgroundJobService _backgroundJobService;

    public BookingService(
        IUnitOfWork unitOfWork,
        IBackgroundJobService backgroundJobService,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _backgroundJobService = backgroundJobService ?? throw new ArgumentNullException(nameof(backgroundJobService));
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

                // 4. Calculate total amount with seat class pricing
                var totalAmount = outboundInventory.CurrentPrice * dto.PassengerCount;

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
                    FinalAmount = totalAmount,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    PromotionId = dto.PromotionId
                };

                var createdBooking = await _unitOfWork.Bookings.CreateAsync(booking);

                // 6. Create passengers
                foreach (var passengerDto in dto.Passengers)
                {
                    var passenger = new BookingPassenger
                    {
                        BookingId = createdBooking.Id,
                        FullName = $"{passengerDto.FirstName} {passengerDto.LastName}".Trim(),
                        DateOfBirth = passengerDto.DateOfBirth,
                        PassengerType = (int)PassengerType.Adult,
                        FlightSeatInventoryId = outboundInventory.Id
                    };

                    await _unitOfWork.BookingPassengers.CreateAsync(passenger);
                }

                // 7. Hold seats atomically within transaction
                outboundInventory.HoldSeats(dto.PassengerCount);
                await _unitOfWork.FlightSeatInventories.UpdateAsync(outboundInventory);

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
                if (previousStatus == (int)BookingStatus.Pending)
                {
                    seatInventory.ReleaseHeldSeats(passengers.Count);
                }
                else
                {
                    seatInventory.CancelSoldSeats(passengers.Count);
                }

                await _unitOfWork.FlightSeatInventories.UpdateAsync(seatInventory);

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

    private async Task<BookingResponse> BuildBookingResponseAsync(Booking booking)
    {
        var outboundFlight = await _unitOfWork.Flights.GetByIdAsync(booking.OutboundFlightId);
        var passengers = await _unitOfWork.BookingPassengers.GetByBookingIdAsync(booking.Id);

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
            FinalAmount = booking.FinalAmount,
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
                FirstName = p.FullName.Split(' ').FirstOrDefault() ?? "",
                LastName = p.FullName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Email = "",
                Phone = "",
                PassportNumber = p.NationalId ?? "",
                Status = "Confirmed"
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

            // Chỉ hỗ trợ VNPay refund hiện tại
            if (!string.Equals(payment.Provider, "VNPAY", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Refund not supported for provider {Provider}", payment.Provider);
                return false;
            }

            // TODO: Khi deploy production với IP whitelist, uncomment code dưới để gọi VNPay API
            /*
            var refundRequest = new VnpayRefundRequest
            {
                TxnRef = payment.TransactionRef,
                TransactionDate = payment.CreatedAt.ToString("yyyyMMddHHmmss"),
                Amount = payment.Amount,
                OrderInfo = $"Hoan tien huy ve - {reason}",
                CreateBy = "SYSTEM"
            };

            var refundResponse = await _vnpayProvider.ProcessRefundAsync(refundRequest);

            if (!refundResponse.Success)
            {
                _logger.LogError("Refund failed for payment {PaymentId}: {ResponseCode} - {Message}", 
                    payment.Id, refundResponse.ResponseCode, refundResponse.Message);
                return false;
            }
            */

            // TEMPORARY: Tạm thời chỉ cập nhật status trong DB, không gọi VNPay API
            _logger.LogWarning("[MOCK REFUND] Skipping VNPay API call (no IP whitelist). Only updating DB status.");
            _logger.LogInformation("[MOCK REFUND] Would refund: TxnRef={TxnRef}, Amount={Amount} VND", 
                payment.TransactionRef, payment.Amount);

            // Cập nhật payment status = Refunded
            payment.Status = (int)PaymentStatus.Refunded;
            payment.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Payments.UpdateAsync(payment);

            _logger.LogInformation("Refund processed successfully for payment {PaymentId} (mock mode)", payment.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", payment.Id);
            return false;
        }
    }
}
