namespace API.Infrastructure.Resilience;

using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

public class ResilientHttpClient : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilientHttpClient> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;

    public ResilientHttpClient(
        HttpClient httpClient,
        ILogger<ResilientHttpClient> logger,
        int maxRetries = 3,
        TimeSpan? baseDelay = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _policy = HttpResiliencePolicy.CreateRetryPolicy(maxRetries, baseDelay, logger);
    }

    /// <summary>
    /// Sends an HTTP request with resilience policy applied
    /// </summary>
    /// <param name="request">HTTP request message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        return await _policy.ExecuteAsync(
            () => _httpClient.SendAsync(request, cancellationToken));
    }

    /// <summary>
    /// Sends an HTTP request with resilience policy applied
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string requestUri,
        HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = content
        };

        return await SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP GET request with resilience policy applied
    /// </summary>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    public async Task<HttpResponseMessage> GetAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(HttpMethod.Get, requestUri, null, cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP POST request with resilience policy applied
    /// </summary>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    public async Task<HttpResponseMessage> PostAsync(
        string requestUri,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(HttpMethod.Post, requestUri, content, cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP PUT request with resilience policy applied
    /// </summary>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    public async Task<HttpResponseMessage> PutAsync(
        string requestUri,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(HttpMethod.Put, requestUri, content, cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP DELETE request with resilience policy applied
    /// </summary>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(HttpMethod.Delete, requestUri, null, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
        _httpClient.Dispose();
    }
}
