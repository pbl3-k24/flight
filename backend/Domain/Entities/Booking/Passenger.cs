using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Booking;

public class Passenger : BaseEntity
{
    public Guid BookingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNumber { get; set; }
    public DateOnly? PassportExpiry { get; set; }
    public string? Nationality { get; set; }
    public PassengerType PassengerType { get; set; } = PassengerType.Adult;

    // Navigation
    public Booking Booking { get; set; } = null!;
    public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
}
