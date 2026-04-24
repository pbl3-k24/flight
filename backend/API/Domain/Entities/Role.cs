namespace API.Domain.Entities;

public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = [];

    public virtual ICollection<UserRole> UserRoles { get; set; } = [];

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}
