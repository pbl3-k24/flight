namespace API.Domain.Entities;

public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = [];

    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}
