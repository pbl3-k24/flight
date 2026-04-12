using API.Domain.Enums;

namespace API.Domain.Entities;

public class Flight
{
    private const decimal MinPriceMultiplier = 0.5m;
    private const decimal MaxPriceMultiplier = 2.0m;

    public int Id { get; set; }

    public string FlightNumber { get; set; } = string.Empty;

    public int DepartureAirportId { get; set; }

    public int ArrivalAirportId { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public decimal BasePrice { get; set; }

    public FlightStatus Status { get; set; } = FlightStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Airport? DepartureAirport { get; set; }

    public Airport? ArrivalAirport { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];

    public ICollection<FlightCrew> FlightCrews { get; set; } = [];

    public bool CanBook(int passengerCount)
    {
        return passengerCount > 0
            && Status == FlightStatus.Active
            && !IsDepartureSoon(2)
            && AvailableSeats >= passengerCount;
    }

    public void ReserveSeats(int count)
    {
        if (!CanBook(count))
        {
            throw new InvalidOperationException("Cannot reserve seats for this flight.");
        }

        AvailableSeats -= count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseSeats(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Seat count must be greater than 0.");
        }

        AvailableSeats = Math.Min(AvailableSeats + count, TotalSeats);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDepartureSoon(int hours)
    {
        return (DepartureTime - DateTime.UtcNow).TotalHours <= hours;
    }

    public bool IsFlightClosed()
    {
        return Status != FlightStatus.Active || IsDepartureSoon(2);
    }

    public decimal CalculatePrice(decimal demandFactor, decimal bookingTimeFactor, decimal seatClassFactor = 1m)
    {
        var rawPrice = BasePrice * demandFactor * bookingTimeFactor * seatClassFactor;
        var minPrice = BasePrice * MinPriceMultiplier;
        var maxPrice = BasePrice * MaxPriceMultiplier;

        return Math.Round(Math.Clamp(rawPrice, minPrice, maxPrice), 2, MidpointRounding.AwayFromZero);
    }

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(FlightNumber) || FlightNumber.Length is < 3 or > 20)
        {
            throw new InvalidOperationException("FlightNumber must be between 3 and 20 characters.");
        }

        if (DepartureAirportId == ArrivalAirportId)
        {
            throw new InvalidOperationException("Departure and arrival airports must be different.");
        }

        if (DepartureTime >= ArrivalTime)
        {
            throw new InvalidOperationException("DepartureTime must be earlier than ArrivalTime.");
        }

        if ((ArrivalTime - DepartureTime).TotalHours < 1)
        {
            throw new InvalidOperationException("Flight duration must be at least 1 hour.");
        }

        if (TotalSeats <= 0)
        {
            throw new InvalidOperationException("TotalSeats must be greater than 0.");
        }

        if (AvailableSeats < 0 || AvailableSeats > TotalSeats)
        {
            throw new InvalidOperationException("AvailableSeats must be between 0 and TotalSeats.");
        }

        if (BasePrice < 0)
        {
            throw new InvalidOperationException("BasePrice cannot be negative.");
        }
    }
}
