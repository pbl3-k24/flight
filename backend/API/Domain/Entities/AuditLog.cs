namespace API.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int? ActorId { get; set; }

    public string Action { get; set; } = null!; // CREATE, UPDATE, DELETE, PAYMENT, REFUND

    public string EntityType { get; set; } = null!; // BOOKING, PAYMENT, REFUND, USER

    public int EntityId { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public DateTime CreatedAt { get; set; }

    // Additional audit properties
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }

    // Navigation properties
    public virtual User? Actor { get; set; }

    // Domain methods
    public string GetChangesSummary()
    {
        return $"{Action} - {EntityType} #{EntityId}";
    }

    public dynamic? GetBeforeState()
    {
        if (string.IsNullOrEmpty(BeforeJson))
            return null;

        return System.Text.Json.JsonSerializer.Deserialize<dynamic>(BeforeJson);
    }

    public dynamic? GetAfterState()
    {
        if (string.IsNullOrEmpty(AfterJson))
            return null;

        return System.Text.Json.JsonSerializer.Deserialize<dynamic>(AfterJson);
    }

    public bool IsCreateAction() => Action == "CREATE";

    public bool IsUpdateAction() => Action == "UPDATE";
}
