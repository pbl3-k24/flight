namespace API.Domain.Entities;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    CheckedIn = 2,
    Cancelled = 3,
    Refunded = 4
}

public enum PassengerType
{
    Adult = 0,
    Child = 1,
    Infant = 2
}
