namespace API.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(BookingRequest request, CancellationToken cancellationToken = default);
}

public sealed class BookingService(ILogger<BookingService> logger) : IBookingService
{
    private readonly ILogger<BookingService> _logger = logger;

    public Task<BookingResponse> CreateBookingAsync(BookingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FlightId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.FlightId), "FlightId must be greater than 0.");
        }

        if (request.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.UserId), "UserId must be greater than 0.");
        }

        if (request.PassengerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PassengerCount), "PassengerCount must be greater than 0.");
        }

        var booking = new BookingResponse(
            BookingReference: Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            FlightId: request.FlightId,
            UserId: request.UserId,
            PassengerCount: request.PassengerCount,
            CreatedAtUtc: DateTime.UtcNow);

        _logger.LogInformation(
            "Booking created successfully. Reference: {BookingReference}, FlightId: {FlightId}, UserId: {UserId}",
            booking.BookingReference,
            booking.FlightId,
            booking.UserId);

        return Task.FromResult(booking);
    }
}

public sealed record BookingRequest(int FlightId, int UserId, int PassengerCount);

public sealed record BookingResponse(
    string BookingReference,
    int FlightId,
    int UserId,
    int PassengerCount,
    DateTime CreatedAtUtc);
