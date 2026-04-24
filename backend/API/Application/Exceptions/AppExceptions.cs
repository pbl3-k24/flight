namespace API.Application.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }

    public AppException(string message, int statusCode = 500, string errorCode = "INTERNAL_ERROR")
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Errors = new Dictionary<string, string[]>();
    }

    public AppException(string message, Dictionary<string, string[]> errors, int statusCode = 400, string errorCode = "VALIDATION_ERROR")
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Errors = errors;
    }
}

public class ValidationException : AppException
{
    public ValidationException(string message, Dictionary<string, string[]> errors = null)
        : base(message, errors ?? new(), 400, "VALIDATION_ERROR")
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, 404, "NOT_FOUND")
    {
    }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(message, 401, "UNAUTHORIZED")
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden")
        : base(message, 403, "FORBIDDEN")
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409, "CONFLICT")
    {
    }
}

public class PaymentException : AppException
{
    public PaymentException(string message)
        : base(message, 402, "PAYMENT_ERROR")
    {
    }
}

public class RateLimitException : AppException
{
    public RateLimitException(string message = "Too many requests")
        : base(message, 429, "RATE_LIMIT_EXCEEDED")
    {
    }
}

public class ConcurrencyException : AppException
{
    public ConcurrencyException(string message = "Concurrency conflict detected")
        : base(message, 409, "CONCURRENCY_CONFLICT")
    {
    }
}