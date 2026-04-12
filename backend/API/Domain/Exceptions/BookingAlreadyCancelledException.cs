namespace API.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to cancel an already cancelled booking.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class BookingAlreadyCancelledException : DomainException
{
    public BookingAlreadyCancelledException(int bookingId)
        : base($"Booking {bookingId} has already been cancelled.") { }
}
