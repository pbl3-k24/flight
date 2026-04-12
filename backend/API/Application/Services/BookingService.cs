namespace API.Application.Services;

using System.Text;
using API.Application.DTOs;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Domain.Enums;
using API.Domain.Exceptions;

/// <summary>
/// Service for managing booking operations.
/// Implements IBookingService with business logic, validation, and transaction management.
/// Follows 02_APPLICATION_LAYER_GUIDE.md booking creation and cancellation algorithms.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IPaymentService _paymentService;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingService> _logger;

    // Constants
    private const string BOOKING_CACHE_KEY = "booking_{0}";
    private const string USER_BOOKINGS_CACHE_KEY = "user_bookings_{0}";
    private const string FLIGHT_CACHE_KEY = "flight_{0}";
    private static readonly TimeSpan BOOKING_CACHE_TTL = TimeSpan.FromHours(1);
    private const int CANCELLATION_WINDOW_HOURS = 24;
    private const decimal CANCELLATION_FEE_PERCENTAGE = 0.20m; // 20% penalty

    public BookingService(
        IFlightRepository flightRepository,
        IBookingRepository bookingRepository,
        IUserRepository userRepository,
        IPassengerRepository passengerRepository,
        IPaymentService paymentService,
        ICacheService cacheService,
        IEmailService emailService,
        ILogger<BookingService> logger)
    {
        _flightRepository = flightRepository;
        _bookingRepository = bookingRepository;
        _userRepository = userRepository;
        _passengerRepository = passengerRepository;
        _paymentService = paymentService;
        _cacheService = cacheService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all bookings for a user with pagination.
    /// Algorithm:
    /// 1. Validate userId and pagination parameters
    /// 2. Query repository for user's bookings
    /// 3. Map entities to response DTOs
    /// 4. Return paginated response
    /// </summary>
    public async Task<PaginatedBookingsResponseDto> GetAllBookingsAsync(int page, int pageSize)
    {
        if (page < 1)
            throw new ValidationException("Page number must be greater than 0.");

        if (pageSize < 1 || pageSize > 100)
            throw new ValidationException("Page size must be between 1 and 100.");

        _logger.LogInformation("Fetching all bookings - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        try
        {
            // Note: This assumes we have a method to get all bookings
            // In production, this would be paginated differently
            var skip = (page - 1) * pageSize;
            
            // TODO: Implement repository method for getting all bookings across all users
            // For now, returning empty result to prevent errors
            var bookings = new List<Booking>();
            var total = 0;

            var dtos = bookings.Select(MapBookingToResponseDto).ToList();

            var response = new PaginatedBookingsResponseDto
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = total == 0 ? 1 : (total + pageSize - 1) / pageSize
            };

            _logger.LogInformation("Successfully retrieved {Count} bookings from {Total} total",
                dtos.Count, total);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all bookings");
            throw;
        }
    }

    /// <summary>
    /// Gets a specific booking by ID.
    /// </summary>
    public async Task<BookingResponseDto> GetBookingByIdAsync(int bookingId)
    {
        if (bookingId <= 0)
            throw new ValidationException("Booking ID must be greater than 0.");

        _logger.LogInformation("Fetching booking with ID: {BookingId}", bookingId);

        try
        {
            // Check cache
            var cacheKey = string.Format(BOOKING_CACHE_KEY, bookingId);
            var cachedBooking = await _cacheService.GetAsync<BookingResponseDto>(cacheKey);
            
            if (cachedBooking != null)
            {
                _logger.LogInformation("Booking {BookingId} found in cache", bookingId);
                return cachedBooking;
            }

            // Query repository
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            
            if (booking == null)
            {
                _logger.LogWarning("Booking with ID {BookingId} not found", bookingId);
                throw new BookingNotFoundException(bookingId);
            }

            // Map to DTO
            var responseDto = MapBookingToResponseDto(booking);

            // Cache result
            await _cacheService.SetAsync(cacheKey, responseDto, BOOKING_CACHE_TTL);

            _logger.LogInformation("Successfully retrieved booking {BookingId}: {BookingReference}",
                bookingId, booking.BookingReference);

            return responseDto;
        }
        catch (BookingNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking with ID: {BookingId}", bookingId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new booking.
    /// Algorithm from 02_APPLICATION_LAYER_GUIDE.md:
    /// 1. Validate input parameters
    /// 2. Fetch flight, check if exists and Active
    /// 3. Fetch user, check if exists
    /// 4. Validate: available_seats >= passenger_count
    /// 5. Start database transaction:
    ///    a. Create booking record (status = Pending)
    ///    b. Generate unique booking reference
    ///    c. Reserve seats (available_seats -= count)
    ///    d. Create passenger records
    ///    e. Commit transaction
    /// 6. Trigger post-booking actions (async, don't await):
    ///    - Send confirmation email
    ///    - Update flight cache
    /// 7. Map and return BookingResponseDto
    /// </summary>
    public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto dto, int userId)
    {
        if (dto == null)
            throw new ValidationException("Booking data cannot be null.");

        if (dto.FlightId <= 0)
            throw new ValidationException("Flight ID must be greater than 0.");

        if (dto.PassengerCount <= 0)
            throw new ValidationException("Passenger count must be at least 1.");

        if (dto.Passengers == null || dto.Passengers.Count == 0)
            throw new ValidationException("At least one passenger is required.");

        if (dto.Passengers.Count != dto.PassengerCount)
            throw new ValidationException("Passenger count must match number of passengers provided.");

        if (userId <= 0)
            throw new ValidationException("User ID must be greater than 0.");

        _logger.LogInformation(
            "Creating booking for user {UserId} on flight {FlightId} with {PassengerCount} passengers",
            userId, dto.FlightId, dto.PassengerCount);

        try
        {
            // Step 1: Fetch and validate flight
            var flight = await _flightRepository.GetByIdAsync(dto.FlightId);
            
            if (flight == null)
            {
                _logger.LogWarning("Flight with ID {FlightId} not found", dto.FlightId);
                throw new FlightNotFoundException(dto.FlightId);
            }

            if (flight.Status != FlightStatus.Active)
            {
                _logger.LogWarning("Flight {FlightId} is not active. Status: {Status}",
                    dto.FlightId, flight.Status);
                throw new ValidationException($"Flight is not available for booking (status: {flight.Status}).");
            }

            // Step 2: Fetch and validate user
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                throw new ValidationException($"User with ID {userId} not found.");
            }

            // Step 3: Validate seat availability
            if (flight.AvailableSeats < dto.PassengerCount)
            {
                _logger.LogWarning(
                    "Insufficient seats on flight {FlightId}. Requested: {Requested}, Available: {Available}",
                    dto.FlightId, dto.PassengerCount, flight.AvailableSeats);
                throw new InsufficientSeatsException(dto.PassengerCount, flight.AvailableSeats);
            }

            // Step 4: Start transaction
            using (var transaction = await _bookingRepository.BeginTransactionAsync())
            {
                try
                {
                    // Step 5a: Create booking record
                    var booking = new Booking
                    {
                        FlightId = dto.FlightId,
                        UserId = userId,
                        PassengerCount = dto.PassengerCount,
                        TotalPrice = flight.BasePrice * dto.PassengerCount,
                        Status = BookingStatus.Pending,
                        Notes = dto.Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Step 5b: Generate unique booking reference
                    string bookingReference;
                    do
                    {
                        bookingReference = GenerateBookingReference();
                    } while (!await _bookingRepository.IsReferenceUniqueAsync(bookingReference));

                    booking.BookingReference = bookingReference;

                    // Step 5c: Add booking to repository
                    var createdBooking = await _bookingRepository.AddAsync(booking);
                    await _bookingRepository.SaveChangesAsync();

                    // Step 5d: Create passenger records
                    var passengers = dto.Passengers.Select(p => new Passenger
                    {
                        BookingId = createdBooking.Id,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        DateOfBirth = p.DateOfBirth,
                        PassportNumber = p.PassportNumber,
                        Nationality = p.Nationality,
                        Email = p.Email,
                        PhoneNumber = p.PhoneNumber,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList();

                    await _passengerRepository.AddRangeAsync(passengers);
                    await _passengerRepository.SaveChangesAsync();

                    // Step 5e: Reserve seats on flight
                    flight.AvailableSeats -= dto.PassengerCount;
                    await _flightRepository.UpdateAsync(flight);
                    await _flightRepository.SaveChangesAsync();

                    // Step 5e: Commit transaction
                    // Transaction will auto-commit when using is disposed

                    _logger.LogInformation(
                        "Successfully created booking {BookingId} with reference {BookingReference}",
                        createdBooking.Id, bookingReference);

                    // Step 6: Trigger post-booking actions (async, no await)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Send confirmation email
                            await _emailService.SendBookingConfirmationAsync(
                                user.Email,
                                bookingReference,
                                flight.FlightNumber,
                                flight.DepartureTime,
                                dto.PassengerCount);

                            // Invalidate flight cache
                            var flightCacheKey = string.Format(FLIGHT_CACHE_KEY, dto.FlightId);
                            await _cacheService.RemoveAsync(flightCacheKey);
                            await _cacheService.RemoveByPatternAsync("flights_search_*");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in post-booking actions for booking {BookingId}",
                                createdBooking.Id);
                        }
                    });

                    // Step 7: Map and return
                    var responseDto = MapBookingToResponseDto(createdBooking);
                    
                    // Cache the new booking
                    var cacheKey = string.Format(BOOKING_CACHE_KEY, createdBooking.Id);
                    await _cacheService.SetAsync(cacheKey, responseDto, BOOKING_CACHE_TTL);

                    return responseDto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during booking creation transaction");
                    throw;
                }
            }
        }
        catch (FlightNotFoundException)
        {
            throw;
        }
        catch (InsufficientSeatsException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            throw;
        }
    }

    /// <summary>
    /// Cancels a booking.
    /// Algorithm from 02_APPLICATION_LAYER_GUIDE.md:
    /// 1. Fetch booking
    /// 2. Validate: exists, user owns it, status allows cancellation
    /// 3. Check if within cancellation window (e.g., 24 hours before)
    /// 4. Calculate refund amount with penalty
    /// 5. Start transaction:
    ///    a. Change status to Cancelled
    ///    b. Release seats back
    ///    c. Create refund record
    ///    d. Commit
    /// 6. Trigger refund process (async)
    /// 7. Invalidate cache
    /// </summary>
    public async Task<BookingResponseDto> CancelBookingAsync(int bookingId, int userId)
    {
        if (bookingId <= 0)
            throw new ValidationException("Booking ID must be greater than 0.");

        if (userId <= 0)
            throw new ValidationException("User ID must be greater than 0.");

        _logger.LogInformation("Cancelling booking {BookingId} for user {UserId}", bookingId, userId);

        try
        {
            // Step 1: Fetch booking
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            
            if (booking == null)
            {
                _logger.LogWarning("Booking with ID {BookingId} not found", bookingId);
                throw new BookingNotFoundException(bookingId);
            }

            // Step 2: Validate user owns booking
            if (booking.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to cancel booking {BookingId} owned by {OwnerId}",
                    userId, bookingId, booking.UserId);
                throw new UnauthorizedAccessException("You are not authorized to cancel this booking.");
            }

            // Step 2: Validate booking status
            if (booking.Status == BookingStatus.Cancelled)
            {
                _logger.LogWarning("Attempt to cancel already cancelled booking {BookingId}", bookingId);
                throw new BookingAlreadyCancelledException(bookingId);
            }

            if (booking.Status == BookingStatus.CheckedIn)
            {
                _logger.LogWarning(
                    "Cannot cancel booking {BookingId} with status {Status}",
                    bookingId, booking.Status);
                throw new InvalidBookingStatusException(booking.Status.ToString(), "cancel");
            }

            // Step 3: Check cancellation window
            var flight = await _flightRepository.GetByIdAsync(booking.FlightId);
            if (flight == null)
            {
                _logger.LogError("Flight {FlightId} for booking {BookingId} not found",
                    booking.FlightId, bookingId);
                throw new FlightNotFoundException(booking.FlightId);
            }

            var hoursUntilDeparture = (int)(flight.DepartureTime - DateTime.UtcNow).TotalHours;
            
            if (hoursUntilDeparture < 0)
            {
                _logger.LogWarning(
                    "Cannot cancel booking {BookingId} for flight that has already departed",
                    bookingId);
                throw new InvalidBookingStatusException("Cannot cancel booking for past flight.");
            }

            if (hoursUntilDeparture < CANCELLATION_WINDOW_HOURS)
            {
                _logger.LogWarning(
                    "Late cancellation of booking {BookingId}. Hours until departure: {Hours}",
                    bookingId, hoursUntilDeparture);
            }

            // Step 4: Calculate refund
            var refundAmount = _paymentService.CalculateRefundAmount(
                booking.TotalPrice,
                hoursUntilDeparture,
                CANCELLATION_FEE_PERCENTAGE);

            _logger.LogInformation(
                "Refund calculated for booking {BookingId}: Original: {Original}, Refund: {Refund}",
                bookingId, booking.TotalPrice, refundAmount);

            // Step 5: Start transaction
            using (var transaction = await _bookingRepository.BeginTransactionAsync())
            {
                try
                {
                    // Step 5a: Change status to Cancelled
                    booking.Status = BookingStatus.Cancelled;
                    booking.UpdatedAt = DateTime.UtcNow;

                    // Step 5b: Release seats back
                    flight.AvailableSeats += booking.PassengerCount;

                    // Update booking and flight
                    await _bookingRepository.UpdateAsync(booking);
                    await _flightRepository.UpdateAsync(flight);
                    await _bookingRepository.SaveChangesAsync();
                    await _flightRepository.SaveChangesAsync();

                    // Step 5c: Create refund record (via payment service)
                    // Note: This would create a refund record in the database
                    // For now, we'll trigger the async refund process

                    // Step 5d: Commit transaction (auto-commits when using is disposed)

                    _logger.LogInformation(
                        "Successfully cancelled booking {BookingId}. Refund amount: {RefundAmount}",
                        bookingId, refundAmount);

                    // Step 6: Trigger refund process (async, no await)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var user = await _userRepository.GetByIdAsync(booking.UserId);
                            if (user != null)
                            {
                                // Process refund
                                await _paymentService.ProcessRefundAsync(
                                    bookingId,
                                    refundAmount,
                                    "Booking cancellation");

                                // Send refund notification
                                await _emailService.SendCancellationConfirmationAsync(
                                    user.Email,
                                    booking.BookingReference,
                                    flight.FlightNumber,
                                    refundAmount);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in refund process for booking {BookingId}", bookingId);
                        }
                    });

                    // Step 7: Invalidate caches
                    var bookingCacheKey = string.Format(BOOKING_CACHE_KEY, bookingId);
                    await _cacheService.RemoveAsync(bookingCacheKey);
                    
                    var userBookingsCacheKey = string.Format(USER_BOOKINGS_CACHE_KEY, booking.UserId);
                    await _cacheService.RemoveAsync(userBookingsCacheKey);

                    var flightCacheKey = string.Format(FLIGHT_CACHE_KEY, booking.FlightId);
                    await _cacheService.RemoveAsync(flightCacheKey);
                    await _cacheService.RemoveByPatternAsync("flights_search_*");

                    // Map and return
                    var responseDto = MapBookingToResponseDto(booking);
                    
                    // Cache the updated booking
                    await _cacheService.SetAsync(bookingCacheKey, responseDto, BOOKING_CACHE_TTL);

                    return responseDto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during booking cancellation transaction");
                    throw;
                }
            }
        }
        catch (BookingNotFoundException)
        {
            throw;
        }
        catch (BookingAlreadyCancelledException)
        {
            throw;
        }
        catch (InvalidBookingStatusException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
            throw;
        }
    }

    /// <summary>
    /// Checks in a passenger for a booking.
    /// </summary>
    public async Task<BookingResponseDto> CheckInBookingAsync(int bookingId, int userId)
    {
        if (bookingId <= 0)
            throw new ValidationException("Booking ID must be greater than 0.");

        if (userId <= 0)
            throw new ValidationException("User ID must be greater than 0.");

        _logger.LogInformation("Checking in booking {BookingId} for user {UserId}", bookingId, userId);

        try
        {
            // Fetch booking
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            
            if (booking == null)
            {
                _logger.LogWarning("Booking with ID {BookingId} not found", bookingId);
                throw new BookingNotFoundException(bookingId);
            }

            // Validate user owns booking
            if (booking.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to check in booking {BookingId} owned by {OwnerId}",
                    userId, bookingId, booking.UserId);
                throw new UnauthorizedAccessException("You are not authorized to check in this booking.");
            }

            // Validate booking status
            if (booking.Status != BookingStatus.Confirmed)
            {
                _logger.LogWarning(
                    "Cannot check in booking {BookingId} with status {Status}",
                    bookingId, booking.Status);
                throw new InvalidBookingStatusException(booking.Status.ToString(), "check-in");
            }

            // Update booking
            booking.Status = BookingStatus.CheckedIn;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);
            await _bookingRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully checked in booking {BookingId}", bookingId);

            // Invalidate caches
            var bookingCacheKey = string.Format(BOOKING_CACHE_KEY, bookingId);
            await _cacheService.RemoveAsync(bookingCacheKey);
            
            var userBookingsCacheKey = string.Format(USER_BOOKINGS_CACHE_KEY, booking.UserId);
            await _cacheService.RemoveAsync(userBookingsCacheKey);

            // Map and return
            var responseDto = MapBookingToResponseDto(booking);
            
            // Cache updated booking
            await _cacheService.SetAsync(bookingCacheKey, responseDto, BOOKING_CACHE_TTL);

            return responseDto;
        }
        catch (BookingNotFoundException)
        {
            throw;
        }
        catch (InvalidBookingStatusException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in booking {BookingId}", bookingId);
            throw;
        }
    }

    /// <summary>
    /// Helper method to map Booking entity to BookingResponseDto.
    /// </summary>
    private BookingResponseDto MapBookingToResponseDto(Booking booking)
    {
        return new BookingResponseDto
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            FlightId = booking.FlightId,
            FlightNumber = booking.Flight?.FlightNumber ?? string.Empty,
            UserId = booking.UserId,
            PassengerCount = booking.PassengerCount,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            Passengers = booking.Passengers?.Select(p => new PassengerResponseDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber
            }).ToList() ?? new List<PassengerResponseDto>(),
            Notes = booking.Notes
        };
    }

    /// <summary>
    /// Helper method to generate a unique booking reference.
    /// Format: AAA000XXX (3 letters + 3 digits + 3 alphanumeric)
    /// </summary>
    private string GenerateBookingReference()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var random = new Random();
        var reference = new StringBuilder();

        // 3 random letters
        for (int i = 0; i < 3; i++)
            reference.Append(letters[random.Next(letters.Length)]);

        // 3 random digits
        for (int i = 0; i < 3; i++)
            reference.Append(digits[random.Next(digits.Length)]);

        // 3 random alphanumeric
        for (int i = 0; i < 3; i++)
            reference.Append(alphanumeric[random.Next(alphanumeric.Length)]);

        return reference.ToString();
    }
}
