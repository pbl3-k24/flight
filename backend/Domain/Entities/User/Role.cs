using Domain.Common;

namespace Domain.Entities.User;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;       // Admin, Staff, Customer
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
