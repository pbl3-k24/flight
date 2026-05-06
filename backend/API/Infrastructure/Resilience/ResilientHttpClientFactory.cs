namespace API.Infrastructure.Resilience;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.RateLimit;
using Polly.Timeout;

public static class ResilientHttpClientFactory
{
    public static IServiceCollection AddResilientHttpClientService(
        this IServiceCollection services,
        string httpClientName,
        int maxRetries = 3,
        Action<HttpClient>? configureHttpClient = null)
    {
        var builder = services.AddHttpClient(httpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "FlightBookingAPI/1.0");
        });

        if (configureHttpClient != null)
        {
            builder.ConfigureHttpClient(configureHttpClient);
        }

        builder.AddPolicyHandler(GetRetryPolicy(maxRetries))
               .AddPolicyHandler(GetCircuitBreakerPolicy())
               .AddPolicyHandler(GetTimeoutPolicy());

        services.AddScoped<ResilientHttpClient>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }
}
