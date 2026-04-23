namespace API.Domain.Booking;

public class Passenger
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string? IdNumber { get; set; }

    public PassengerType PassengerType { get; set; } = PassengerType.Adult;
}
