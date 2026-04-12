namespace API.Domain.Exceptions;

/// <summary>
/// Exception thrown when a booking is not found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class BookingNotFoundException : NotFoundException
{
    public BookingNotFoundException(int bookingId)
        : base("Booking", bookingId) { }
}
