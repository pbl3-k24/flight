namespace API.Domain.Entities;

/// <summary>
/// Represents an authorization role in the system.
/// </summary>
public class Role
{
    /// <summary>Unique identifier for the role.</summary>
    public int Id { get; set; }

    /// <summary>Unique role name (e.g. Admin, Customer).</summary>
    public string Name { get; set; } = null!;

    /// <summary>Role description.</summary>
    public string? Description { get; set; }

    /// <summary>Users assigned to this role.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
