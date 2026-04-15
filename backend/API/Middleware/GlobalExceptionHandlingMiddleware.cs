namespace API.Middleware;

using API.Application.Exceptions;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

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
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path
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
                context.Response.Headers.Add("Retry-After", "60");
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

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string Path { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }
    }
}
