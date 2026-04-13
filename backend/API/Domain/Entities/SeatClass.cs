using System.ComponentModel.DataAnnotations;

namespace API.Domain.Entities;

public class SeatClass
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public decimal RefundPercent { get; set; }
    public decimal ChangeFee { get; set; }
    public int Priority { get; set; }

    public ICollection<AircraftSeatTemplate> AircraftSeatTemplates { get; set; } = new List<AircraftSeatTemplate>();
    public ICollection<FlightSeatInventory> FlightSeatInventories { get; set; } = new List<FlightSeatInventory>();
    public ICollection<RefundPolicy> RefundPolicies { get; set; } = new List<RefundPolicy>();
}
