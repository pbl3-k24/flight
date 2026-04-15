namespace API.Domain.Entities;

public class SeatClass
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal RefundPercent { get; set; }

    public decimal ChangeFee { get; set; }

    public int Priority { get; set; } // 1=First, 2=Business, 3=Economy

    // Navigation properties
    public virtual ICollection<AircraftSeatTemplate> AircraftSeatTemplates { get; set; } = [];

    public virtual ICollection<FlightSeatInventory> FlightSeatInventories { get; set; } = [];

    public virtual ICollection<RefundPolicy> RefundPolicies { get; set; } = [];
}
