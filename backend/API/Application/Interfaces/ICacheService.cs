namespace API.Application.Interfaces;

/// <summary>
/// Cache service interface for distributed caching operations.
/// Provides methods for storing, retrieving, and invalidating cached data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache by key.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key to retrieve.</param>
    /// <returns>Cached value if found, null otherwise.</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a value in the cache with optional TTL (time to live).
    /// </summary>
    /// <typeparam name="T">Type of the value to cache.</typeparam>
    /// <param name="key">Cache key to store.</param>
    /// <param name="value">Value to store in cache.</param>
    /// <param name="ttl">Time to live duration. Default: 1 hour.</param>
    /// <returns>Completed task.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class;

    /// <summary>
    /// Removes a value from the cache by key.
    /// </summary>
    /// <param name="key">Cache key to remove.</param>
    /// <returns>Completed task.</returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes all cached values matching a pattern.
    /// </summary>
    /// <param name="pattern">Pattern to match cache keys (e.g., "flight_*").</param>
    /// <returns>Completed task.</returns>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">Cache key to check.</param>
    /// <returns>True if key exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    /// <returns>Completed task.</returns>
    Task ClearAllAsync();
}
