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

    public BookingService(
        IUnitOfWork unitOfWork,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BookingResponse> CreateBookingAsync(int userId, CreateBookingDto dto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // 1. Validate flight exists
            var outboundFlight = await _unitOfWork.Flights.GetByIdAsync(dto.OutboundFlightId);
            if (outboundFlight == null)
            {
                throw new NotFoundException("Flight not found");
            }

            // 2. Validate passenger count
            if (dto.Passengers.Count != dto.PassengerCount || dto.PassengerCount <= 0 || dto.PassengerCount > 9)
            {
                throw new ValidationException("Invalid passenger count");
            }

            // 3. Validate seats available
            var outboundInventory = await _unitOfWork.FlightSeatInventories.GetByFlightAndSeatClassAsync(
                dto.OutboundFlightId, dto.SeatClassId);
            if (outboundInventory == null || outboundInventory.AvailableSeats < dto.PassengerCount)
            {
                throw new ValidationException("Insufficient seats available");
            }

            // 4. Calculate total amount with seat class pricing
            var totalAmount = outboundInventory.CurrentPrice * dto.PassengerCount;

            // 5. Create booking with expiration
            var booking = new Booking
            {
                UserId = userId,
                BookingCode = GenerateBookingCode(),
                OutboundFlightId = dto.OutboundFlightId,
                ReturnFlightId = dto.ReturnFlightId,
                Status = (int)BookingStatus.Pending,
                ContactEmail = dto.ContactEmail ?? "",
                TotalAmount = totalAmount,
                FinalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
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

            // Commit transaction - all or nothing
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Booking created atomically: {BookingCode}", booking.BookingCode);

            return await BuildBookingResponseAsync(createdBooking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking - rolling back transaction");
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CancelBookingAsync(int bookingId, int userId, string reason)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
            {
                throw new UnauthorizedException("Cannot cancel this booking");
            }

            if (booking.Status != (int)BookingStatus.Confirmed)
            {
                throw new ValidationException("Only confirmed bookings can be cancelled");
            }

            var flight = await _unitOfWork.Flights.GetByIdAsync(booking.OutboundFlightId);
            var hoursToDeparture = (flight!.DepartureTime - DateTime.UtcNow).TotalHours;

            if (hoursToDeparture < 24)
            {
                throw new ValidationException("Cannot cancel within 24 hours of departure");
            }

            // Get seat inventory BEFORE updating booking status
            var passengers = await _unitOfWork.BookingPassengers.GetByBookingIdAsync(bookingId);
            var seatInventory = await _unitOfWork.FlightSeatInventories.GetByIdAsync(
                passengers.First().FlightSeatInventoryId);

            // Release seats based on ORIGINAL booking status (before update)
            if (seatInventory != null)
            {
                if (booking.Status == (int)BookingStatus.Confirmed)
                {
                    seatInventory.CancelSoldSeats(passengers.Count);
                }
                else if (booking.Status == (int)BookingStatus.Pending)
                {
                    seatInventory.ReleaseHeldSeats(passengers.Count);
                }
                await _unitOfWork.FlightSeatInventories.UpdateAsync(seatInventory);
            }

            // Update booking status AFTER releasing seats
            booking.Status = (int)BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.UpdateAsync(booking);

            _logger.LogInformation("Booking cancelled: {BookingId} with {PassengerCount} passengers", 
                bookingId, passengers.Count);
            return true;
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
}
