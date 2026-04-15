namespace API.Domain.Entities;

public class BookingPassenger
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? NationalId { get; set; }

    public int PassengerType { get; set; } = 0; // 0=Adult, 1=Child, 2=Infant

    public int FlightSeatInventoryId { get; set; }

    public string? FareSnapshot { get; set; }

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;

    public virtual FlightSeatInventory FlightSeatInventory { get; set; } = null!;

    public virtual ICollection<BookingService> Services { get; set; } = [];

    public virtual Ticket? Ticket { get; set; }

    // Domain methods
    public bool IsAdult() => PassengerType == 0;

    public bool IsChild() => PassengerType == 1;

    public bool IsInfant() => PassengerType == 2;

    public int GetAge()
    {
        if (!DateOfBirth.HasValue)
            return 0;

        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Value.Year;
        if (DateOfBirth.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }

    public dynamic? GetFareDetails()
    {
        if (string.IsNullOrEmpty(FareSnapshot))
            return null;

        return System.Text.Json.JsonSerializer.Deserialize<dynamic>(FareSnapshot);
    }
}
