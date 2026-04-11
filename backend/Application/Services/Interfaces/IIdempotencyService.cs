namespace FlightBooking.Application.Services.Interfaces;

public interface IIdempotencyService
{
    /// <summary>Returns true if this key has already been processed. Stores the key if new.</summary>
    Task<bool> IsAlreadyProcessedAsync(string key, string operationType);

    /// <summary>Store the result payload for an idempotency key so it can be replayed on duplicate calls.</summary>
    Task SetResponseAsync(string key, string operationType, string responsePayload, TimeSpan ttl);

    /// <summary>Retrieve a stored response for a duplicate request.</summary>
    Task<string?> GetResponseAsync(string key, string operationType);
}
