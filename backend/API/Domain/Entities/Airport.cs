using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class Airport
{
    public int Id { get; set; }

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Province { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Route> DepartureRoutes { get; set; } = new List<Route>();
    public ICollection<Route> ArrivalRoutes { get; set; } = new List<Route>();

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
