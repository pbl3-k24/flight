namespace API.Infrastructure.Security;

using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System;

public interface IDataProtectionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public class DataProtectionService : IDataProtectionService
{
    private readonly IDataProtector _protector;

    public DataProtectionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FlightBooking.DataProtection");
    }

    public string Encrypt(string plaintext)
    {
        try
        {
            return _protector.Protect(plaintext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Data encryption failed", ex);
        }
    }

    public string Decrypt(string ciphertext)
    {
        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Data decryption failed", ex);
        }
    }
}

public class AuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditLogRepository auditLogRepository, ILogger<AuditService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task LogActionAsync(
        int? userId,
        string action,
        string entity,
        int? entityId,
        string oldValues = null,
        string newValues = null,
        string ipAddress = "Unknown")
    {
        try
        {
            var auditLog = new AuditLog
            {
                ActorId = userId,
                Action = action,
                EntityType = entity,
                EntityId = entityId ?? 0,
                BeforeJson = oldValues,
                AfterJson = newValues,
                CreatedAt = DateTime.UtcNow
            };

            await _auditLogRepository.CreateAsync(auditLog);
            _logger.LogInformation("Audit logged: {Action} on {Entity} by user {UserId}", action, entity, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit action");
        }
    }

    public async Task LogSecurityEventAsync(
        int? userId,
        string eventType,
        string details,
        string ipAddress)
    {
        _logger.LogWarning(
            "Security Event: {EventType} for user {UserId} from {IpAddress} - {Details}",
            eventType,
            userId,
            ipAddress,
            details);

        // Could also store in dedicated security events table
        await LogActionAsync(userId, $"SECURITY_{eventType}", "SECURITY", null, details, null, ipAddress);
    }
}
