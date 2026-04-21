namespace API.Middleware;

using API.Application.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception {TraceId} at {Method} {Path}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;

        var response = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            TraceId = context.TraceIdentifier
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = 400;
                response.Message = validationEx.Message;
                response.ErrorCode = validationEx.ErrorCode;
                response.Errors = validationEx.Errors;
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusCode = 404;
                response.Message = notFoundEx.Message;
                response.ErrorCode = notFoundEx.ErrorCode;
                break;

            case UnauthorizedException unauthorizedEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.StatusCode = 401;
                response.Message = unauthorizedEx.Message;
                response.ErrorCode = unauthorizedEx.ErrorCode;
                break;

            case ForbiddenException forbiddenEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.StatusCode = 403;
                response.Message = forbiddenEx.Message;
                response.ErrorCode = forbiddenEx.ErrorCode;
                break;

            case ConflictException conflictEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.StatusCode = 409;
                response.Message = conflictEx.Message;
                response.ErrorCode = conflictEx.ErrorCode;
                break;

            case RateLimitException rateLimitEx:
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                response.StatusCode = 429;
                response.Message = rateLimitEx.Message;
                response.ErrorCode = rateLimitEx.ErrorCode;
                context.Response.Headers["Retry-After"] = "60";
                break;

            case PaymentException paymentEx:
                context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                response.StatusCode = 402;
                response.Message = paymentEx.Message;
                response.ErrorCode = paymentEx.ErrorCode;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = 500;
                response.Message = "An unexpected error occurred";
                response.ErrorCode = "INTERNAL_ERROR";
                break;
        }

        var json = JsonSerializer.Serialize(response, options);
        return context.Response.WriteAsync(json);
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }= string.Empty;
        public string ErrorCode { get; set; }= string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TraceId { get; set; } = string.Empty;

        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
