namespace FlightBooking.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // admin, user
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
