namespace API.Infrastructure.Caching;

using System.Collections.Concurrent;
using System.Text.Json;
using API.Application.Interfaces;

/// <summary>
/// In-memory cache service implementation.
/// Provides distributed caching with serialization/deserialization support.
/// Suitable for single-instance deployments. For multi-instance, use Redis instead.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    /// <summary>
    /// Internal cache store with TTL tracking.
    /// </summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    /// Logger for cache operations.
    /// </summary>
    private readonly ILogger<InMemoryCacheService> _logger;

    /// <summary>
    /// Default cache time-to-live (1 hour).
    /// </summary>
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    /// <summary>
    /// JSON serializer options.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Timer for cleanup of expired entries.
    /// </summary>
    private Timer? _cleanupTimer;

    /// <summary>
    /// Internal class to store cache entries with expiration.
    /// </summary>
    private class CacheEntry
    {
        public string Value { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }

    /// <summary>
    /// Initializes a new instance of the InMemoryCacheService class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Start cleanup timer (run every 5 minutes)
        _cleanupTimer = new Timer(
            _ => CleanupExpiredEntries(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("InMemoryCacheService initialized");
    }

    /// <summary>
    /// Gets a cached value by key with deserialization.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key to retrieve.</param>
    /// <returns>Deserialized cached value if found and not expired, null otherwise.</returns>
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("GetAsync called with null or empty key");
            return Task.FromResult<T?>(null);
        }

        try
        {
            _logger.LogDebug("Getting cache value for key: {Key}", key);

            if (!_cache.TryGetValue(key, out var entry))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return Task.FromResult<T?>(null);
            }

            // Check if expired
            if (entry.IsExpired)
            {
                _logger.LogDebug("Cache entry expired for key: {Key}", key);
                _cache.TryRemove(key, out _);
                return Task.FromResult<T?>(null);
            }

            // Deserialize from JSON
            var deserialized = JsonSerializer.Deserialize<T>(entry.Value, JsonOptions);

            if (deserialized != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
            }

            return Task.FromResult(deserialized);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed for cache key: {Key}", key);
            _cache.TryRemove(key, out _);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting cache value for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    /// <summary>
    /// Sets a cached value with optional TTL.
    /// </summary>
    /// <typeparam name="T">Type of the value to cache.</typeparam>
    /// <param name="key">Cache key to store.</param>
    /// <param name="value">Value to store in cache.</param>
    /// <param name="ttl">Time to live. Defaults to 1 hour if not specified.</param>
    /// <returns>Completed task.</returns>
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("SetAsync called with null or empty key");
            return Task.CompletedTask;
        }

        if (value == null)
        {
            _logger.LogWarning("SetAsync called with null value for key: {Key}", key);
            return Task.CompletedTask;
        }

        try
        {
            var expiration = ttl ?? DefaultTtl;
            _logger.LogDebug("Setting cache value for key: {Key} with TTL: {TTL}",
                key, expiration);

            // Serialize to JSON
            var serialized = JsonSerializer.Serialize(value, JsonOptions);

            // Create cache entry
            var entry = new CacheEntry
            {
                Value = serialized,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            };

            // Store in cache
            _cache.AddOrUpdate(key, entry, (_, _) => entry);

            _logger.LogDebug("Successfully cached key: {Key} with expiration: {Expiration}",
                key, expiration);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization failed for cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting cache value for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    /// <param name="key">Cache key to remove.</param>
    /// <returns>Completed task.</returns>
    public Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("RemoveAsync called with null or empty key");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogDebug("Removing cache for key: {Key}", key);

            var removed = _cache.TryRemove(key, out _);

            if (removed)
            {
                _logger.LogDebug("Successfully removed cache for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache key not found for removal: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error removing cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all cached values matching a pattern.
    /// </summary>
    /// <param name="pattern">Pattern to match cache keys (e.g., "flight_*", "booking_*").</param>
    /// <returns>Completed task.</returns>
    public Task RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            _logger.LogWarning("RemoveByPatternAsync called with null or empty pattern");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogDebug("Removing cache for pattern: {Pattern}", pattern);

            // Convert pattern to regex (simple glob-style matching)
            var regexPattern = "^" + pattern.Replace("*", ".*").Replace("?", ".") + "$";
            var regex = new System.Text.RegularExpressions.Regex(regexPattern);

            // Find and remove matching keys
            var keysToRemove = _cache.Keys
                .Where(k => regex.IsMatch(k))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }

            _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}",
                keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error removing cache pattern: {Pattern}", pattern);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a cache key exists and is not expired.
    /// </summary>
    /// <param name="key">Cache key to check.</param>
    /// <returns>True if key exists and is not expired, false otherwise.</returns>
    public Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("ExistsAsync called with null or empty key");
            return Task.FromResult(false);
        }

        try
        {
            _logger.LogDebug("Checking existence of cache key: {Key}", key);

            if (!_cache.TryGetValue(key, out var entry))
            {
                return Task.FromResult(false);
            }

            // Check if expired
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult(false);
            }

            _logger.LogDebug("Cache key {Key} exists", key);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking cache key existence: {Key}", key);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    /// <returns>Completed task.</returns>
    public Task ClearAllAsync()
    {
        try
        {
            _logger.LogWarning("Clearing all cache data");

            var count = _cache.Count;
            _cache.Clear();

            _logger.LogInformation("Successfully cleared {Count} cache entries", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error clearing cache");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the TTL (time to live) for a cache key in seconds.
    /// </summary>
    /// <param name="key">Cache key to check.</param>
    /// <returns>TTL in seconds, or -1 if key doesn't exist, or -2 if no expiration is set.</returns>
    public Task<long> GetTtlAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("GetTtlAsync called with null or empty key");
            return Task.FromResult(-1L);
        }

        try
        {
            if (!_cache.TryGetValue(key, out var entry))
            {
                return Task.FromResult(-1L);
            }

            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult(-1L);
            }

            var ttl = (long)(entry.ExpiresAt - DateTime.UtcNow).TotalSeconds;
            return Task.FromResult(ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for cache key: {Key}", key);
            return Task.FromResult(-1L);
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Dictionary with cache statistics.</returns>
    public Task<Dictionary<string, string>> GetStatsAsync()
    {
        var stats = new Dictionary<string, string>
        {
            ["total_entries"] = _cache.Count.ToString(),
            ["cache_type"] = "InMemory",
            ["default_ttl"] = DefaultTtl.TotalSeconds.ToString(),
            ["cleanup_interval_minutes"] = "5"
        };

        // Count expired entries
        var expiredCount = _cache.Values.Count(e => e.IsExpired);
        stats["expired_entries"] = expiredCount.ToString();

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Internal method to clean up expired entries.
    /// </summary>
    private void CleanupExpiredEntries()
    {
        try
        {
            var keysToRemove = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    /// <summary>
    /// Disposes the cache service and stops the cleanup timer.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cache.Clear();
        GC.SuppressFinalize(this);
    }
}
