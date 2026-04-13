namespace API.Domain.Entities;

/// <summary>
/// Many-to-many join entity between users and roles.
/// </summary>
public class UserRole
{
    /// <summary>User identifier.</summary>
    public int UserId { get; set; }

    /// <summary>Role identifier.</summary>
    public int RoleId { get; set; }

    /// <summary>User navigation.</summary>
    public User? User { get; set; }

    /// <summary>Role navigation.</summary>
    public Role? Role { get; set; }
}
