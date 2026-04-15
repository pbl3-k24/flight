namespace API.Middleware;

using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Log request
        _logger.LogInformation(
            "HTTP Request: {Method} {Path} - Remote IP: {RemoteIp}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            _logger.LogInformation(
                "HTTP Response: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow Request: {Method} {Path} - Duration: {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // Prevent content sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // Enable XSS protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        // Referrer policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");

        // Remove sensitive headers
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly Dictionary<string, RateLimitData> IpRequests = new();
    private const int MaxRequests = 100;
    private const int WindowSeconds = 60;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!IpRequests.ContainsKey(ipAddress))
        {
            IpRequests[ipAddress] = new RateLimitData { Count = 1, ResetTime = DateTime.UtcNow.AddSeconds(WindowSeconds) };
        }
        else
        {
            var data = IpRequests[ipAddress];

            if (DateTime.UtcNow > data.ResetTime)
            {
                data.Count = 1;
                data.ResetTime = DateTime.UtcNow.AddSeconds(WindowSeconds);
            }
            else
            {
                data.Count++;

                if (data.Count > MaxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers.Add("Retry-After", WindowSeconds.ToString());
                    await context.Response.WriteAsJsonAsync(new { message = "Rate limit exceeded" });
                    return;
                }
            }
        }

        await _next(context);
    }

    private class RateLimitData
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }
}
