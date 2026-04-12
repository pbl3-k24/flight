namespace API.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, int id)
        : base($"{resourceName} with ID {id} was not found.") { }

    public NotFoundException(string resourceName, string identifier)
        : base($"{resourceName} '{identifier}' was not found.") { }
}
