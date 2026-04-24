namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IAuditLogRepository
{
    Task<AuditLog?> GetByIdAsync(int id);

    Task<IEnumerable<AuditLog>> GetByEntityTypeAsync(string entityType);

    Task<IEnumerable<AuditLog>> GetByActionAsync(string action);

    Task<IEnumerable<AuditLog>> GetAllAsync();

    Task<AuditLog> CreateAsync(AuditLog auditLog);

    Task DeleteAsync(int id);
}