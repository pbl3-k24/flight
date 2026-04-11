namespace FlightBooking.Domain.Entities;

public class Airport
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // IATA: SGN, HAN, DAD
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "VN";
    public string? Timezone { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Route> DepartureRoutes { get; set; } = [];
    public ICollection<Route> ArrivalRoutes { get; set; } = [];
}
