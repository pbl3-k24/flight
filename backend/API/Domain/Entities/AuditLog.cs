using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace API.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int? ActorId { get; set; }

    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Actor { get; set; }

    public string GetChangesSummary() => $"{Action} {EntityType}#{EntityId}";

    public object? GetBeforeState()
        => string.IsNullOrWhiteSpace(BeforeJson) ? null : JsonSerializer.Deserialize<object>(BeforeJson);

    public object? GetAfterState()
        => string.IsNullOrWhiteSpace(AfterJson) ? null : JsonSerializer.Deserialize<object>(AfterJson);

    public bool IsCreateAction() => string.Equals(Action, "CREATE", StringComparison.OrdinalIgnoreCase);

    public bool IsUpdateAction() => string.Equals(Action, "UPDATE", StringComparison.OrdinalIgnoreCase);
}
