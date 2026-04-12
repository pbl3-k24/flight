namespace API.Domain.Exceptions;

/// <summary>
/// Exception thrown when input validation fails.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : DomainException
{
    public ValidationException(string message)
        : base(message) { }

    public ValidationException(string field, string message)
        : base($"Validation error in {field}: {message}") { }
}
