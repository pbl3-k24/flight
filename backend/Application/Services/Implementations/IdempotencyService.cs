using FlightBooking.Application.Services.Interfaces;

namespace FlightBooking.Application.Services.Implementations;

public class IdempotencyService(Domain.Interfaces.Repositories.IIdempotencyKeyRepository repository) : IIdempotencyService
{
    public async Task<bool> IsAlreadyProcessedAsync(string key, string operationType)
    {
        var existing = await repository.GetAsync(key, operationType);
        return existing is not null && existing.ExpiresAt > DateTime.UtcNow;
    }

    public async Task SetResponseAsync(string key, string operationType, string responsePayload, TimeSpan ttl)
    {
        var record = new Domain.Entities.IdempotencyKey
        {
            Id = Guid.NewGuid(),
            Key = key,
            OperationType = operationType,
            ResponsePayload = responsePayload,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };
        await repository.AddAsync(record);
        await repository.SaveChangesAsync();
    }

    public async Task<string?> GetResponseAsync(string key, string operationType)
    {
        var record = await repository.GetAsync(key, operationType);
        return record?.ExpiresAt > DateTime.UtcNow ? record.ResponsePayload : null;
    }
}
