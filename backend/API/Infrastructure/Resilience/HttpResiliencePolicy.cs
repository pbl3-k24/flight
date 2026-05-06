namespace API.Infrastructure.Resilience;

using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

public static class HttpResiliencePolicy
{
    /// <summary>
    /// Creates a Wait and Retry policy with exponential backoff for handling 429 Too Many Requests
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries (default: 3)</param>
    /// <param name="baseDelay">Base delay for exponential backoff (default: 1 second)</param>
    /// <param name="logger">Optional logger for tracing retry attempts</param>
    /// <returns>RetryPolicy configured for 429 handling with Retry-After header support</returns>
    public static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy(
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        ILogger? logger = null)
    {
        baseDelay ??= TimeSpan.FromSeconds(1);

        return Policy<HttpResponseMessage>
            .HandleResult(response => response.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: (retryAttempt, outcome, context) =>
                {
                    // Get Retry-After header value if present
                    if (outcome.Result?.Headers?.RetryAfter?.Date is { } retryAfterDate)
                    {
                        var headerDelay = retryAfterDate - DateTime.UtcNow;
                        return headerDelay > TimeSpan.Zero ? headerDelay : baseDelay.Value;
                    }

                    if (outcome.Result?.Headers?.RetryAfter?.Delta is { } retryAfterDelta)
                    {
                        return retryAfterDelta;
                    }

                    var exponentialDelay = baseDelay.Value * (int)Math.Pow(2, retryAttempt);

                    logger?.LogWarning(
                        "Retry {Attempt}/{MaxRetries}: Received 429 Too Many Requests. " +
                        "No Retry-After header found. Using exponential backoff: {Delay}ms",
                        retryAttempt, maxRetries, exponentialDelay.TotalMilliseconds);

                    return exponentialDelay;
                },
                onRetryAsync: (outcome, timespan, retryAttempt, context) =>
                {
                    logger?.LogWarning(
                        "Retry {Attempt}/{MaxRetries}: Waiting {Delay}ms before retry. " +
                        "Request: {Method} {Uri}",
                        retryAttempt, maxRetries, timespan.TotalMilliseconds,
                        outcome.Result?.RequestMessage?.Method,
                        outcome.Result?.RequestMessage?.RequestUri?.ToString() ?? "Unknown");
                    
                    return Task.CompletedTask;
                });
    }
}
