namespace API.Infrastructure.Resilience;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using Polly;

public static class HttpClientResilienceExtensions
{
    /// <summary>
    /// Adds resilience policy to HttpClient registration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="httpClientName">Name of the HttpClient</param>
    /// <param name="maxRetries">Maximum number of retries (default: 3)</param>
    /// <param name="baseDelay">Base delay for exponential backoff (default: 1 second)</param>
    /// <param name="configureHttpClient">Optional HttpClient configuration</param>
    /// <returns>IServiceCollection for chaining</returns>
    public static IServiceCollection AddResilientHttpClient(
        this IServiceCollection services,
        string httpClientName,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        Action<HttpClient>? configureHttpClient = null)
    {
        services.AddHttpClient(httpClientName, configureHttpClient)
            .AddPolicyHandler((sp, _) =>
            {
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("API.Infrastructure.Resilience.HttpResiliencePolicy");
                return HttpResiliencePolicy.CreateRetryPolicy(maxRetries, baseDelay, logger);
            });

        return services;
    }

    /// <summary>
    /// Adds resilience policy to all HttpClient registrations
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="maxRetries">Maximum number of retries (default: 3)</param>
    /// <param name="baseDelay">Base delay for exponential backoff (default: 1 second)</param>
    /// <returns>IServiceCollection for chaining</returns>
    public static IServiceCollection AddGlobalResiliencePolicy(
        this IServiceCollection services,
        int maxRetries = 3,
        TimeSpan? baseDelay = null)
    {
        services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
            .AddPolicyHandler((sp, _) =>
            {
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("API.Infrastructure.Resilience.HttpResiliencePolicy");
                return HttpResiliencePolicy.CreateRetryPolicy(maxRetries, baseDelay, logger);
            });

        return services;
    }
}
