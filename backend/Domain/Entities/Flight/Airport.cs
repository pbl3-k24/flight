using Domain.Common;

namespace Domain.Entities.Flight;

public class Airport : BaseEntity
{
    public string IataCode { get; set; } = string.Empty;  // VD: SGN, HAN, DAD
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Vietnam";
    public string? TimeZone { get; set; }

    // Navigation
    public ICollection<Route> OriginRoutes { get; set; } = new List<Route>();
    public ICollection<Route> DestinationRoutes { get; set; } = new List<Route>();
}
