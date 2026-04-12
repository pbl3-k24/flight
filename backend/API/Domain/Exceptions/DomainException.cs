namespace API.Domain.Exceptions;

/// <summary>
/// Base exception for all application exceptions.
/// Inherits from ApplicationException for consistency.
/// </summary>
public class DomainException : ApplicationException
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
