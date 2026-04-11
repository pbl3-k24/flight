namespace FlightBooking.Domain.Entities;

public class Passenger
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string IdentityNumber { get; set; } = string.Empty;
    public string PassengerType { get; set; } = "adult"; // adult, child, infant
    public string? PassportNumber { get; set; }
    public DateTime? PassportExpiry { get; set; }

    public Booking Booking { get; set; } = null!;
    public ICollection<BookingItem> BookingItems { get; set; } = [];
}
