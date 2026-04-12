namespace API.Domain.Exceptions;

/// <summary>
/// Exception thrown when a flight is not found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class FlightNotFoundException : NotFoundException
{
    public FlightNotFoundException(int flightId)
        : base("Flight", flightId) { }
}
