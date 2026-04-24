namespace API.Domain.Entities;

public class Airport
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string City { get; set; } = null!;

    public string? Province { get; set; }

    public bool IsActive { get; set; } = true;

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Route> DepartureRoutes { get; set; } = [];

    public virtual ICollection<Route> ArrivalRoutes { get; set; } = [];

    // Domain methods
    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

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
