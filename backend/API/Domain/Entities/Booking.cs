using API.Domain.Enums;

namespace API.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public int FlightId { get; set; }

    public int UserId { get; set; }

    public string BookingReference { get; set; } = string.Empty;

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public decimal TotalPrice { get; set; }

    public DateTime BookedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAt { get; set; }

    public Flight? Flight { get; set; }

    public User? User { get; set; }

    public Payment? Payment { get; set; }

    public ICollection<Passenger> Passengers { get; set; } = [];

    public int PassengerCount => Passengers.Count;

    public bool CanCancel(DateTime currentUtc)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return false;
        }

        if (Flight is null)
        {
            return false;
        }

        return !Flight.IsFlightClosed() && currentUtc < Flight.DepartureTime.AddHours(-24);
    }

    public void Confirm(DateTime currentUtc)
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only pending bookings can be confirmed.");
        }

        Status = BookingStatus.Confirmed;
        UpdatedAt = currentUtc;
    }

    public void Cancel(DateTime currentUtc)
    {
        if (!CanCancel(currentUtc))
        {
            throw new InvalidOperationException("Booking cannot be cancelled.");
        }

        Status = BookingStatus.Cancelled;
        CancelledAt = currentUtc;
        UpdatedAt = currentUtc;
    }

    public void CheckIn(DateTime currentUtc)
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed bookings can be checked in.");
        }

        Status = BookingStatus.CheckedIn;
        UpdatedAt = currentUtc;
    }

    public decimal CalculateRefund()
    {
        if (Status != BookingStatus.Cancelled || CancelledAt is null || Flight is null)
        {
            return 0m;
        }

        var hoursTillFlight = (Flight.DepartureTime - CancelledAt.Value).TotalHours;
        var feePercentage = hoursTillFlight switch
        {
            > 72 => 0m,
            > 24 => 10m,
            _ => 25m
        };

        var refund = TotalPrice * (1 - (feePercentage / 100));
        return Math.Round(Math.Max(refund, 0m), 2, MidpointRounding.AwayFromZero);
    }

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(BookingReference))
        {
            throw new InvalidOperationException("BookingReference is required.");
        }

        if (TotalPrice <= 0)
        {
            throw new InvalidOperationException("TotalPrice must be greater than 0.");
        }
    }
}
