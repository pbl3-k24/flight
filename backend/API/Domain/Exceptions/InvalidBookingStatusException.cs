namespace API.Domain.Exceptions;

/// <summary>
/// Exception thrown when a booking operation has invalid state.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class InvalidBookingStatusException : DomainException
{
    public InvalidBookingStatusException(string message)
        : base(message) { }

    public InvalidBookingStatusException(string status, string operation)
        : base($"Cannot {operation} booking with status '{status}'.") { }
}
