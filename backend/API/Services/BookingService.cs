namespace API.Services;

public interface IBookingService
{
    BookingResponse CreateBooking(BookingRequest request);
}

public sealed class BookingService(ILogger<BookingService> logger) : IBookingService
{
    private const int BookingReferenceLength = 8;

    private readonly ILogger<BookingService> _logger = logger;

    public BookingResponse CreateBooking(BookingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var booking = new BookingResponse(
            BookingReference: Guid.NewGuid().ToString("N")[..BookingReferenceLength].ToUpperInvariant(),
            FlightId: request.FlightId,
            UserId: request.UserId,
            PassengerCount: request.PassengerCount,
            CreatedAtUtc: DateTime.UtcNow);

        _logger.LogInformation(
            "Booking created successfully. Reference: {BookingReference}, FlightId: {FlightId}, UserId: {UserId}",
            booking.BookingReference,
            booking.FlightId,
            booking.UserId);

        return booking;
    }
}

public sealed record BookingRequest(int FlightId, int UserId, int PassengerCount);

public sealed record BookingResponse(
    string BookingReference,
    int FlightId,
    int UserId,
    int PassengerCount,
    DateTime CreatedAtUtc);
