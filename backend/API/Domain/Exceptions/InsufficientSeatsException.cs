namespace API.Domain.Exceptions;

/// <summary>
/// Exception thrown when a flight has insufficient seats for a booking.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class InsufficientSeatsException : DomainException
{
    public InsufficientSeatsException(int requestedSeats, int availableSeats)
        : base($"Insufficient seats available. Requested: {requestedSeats}, Available: {availableSeats}") { }
}
