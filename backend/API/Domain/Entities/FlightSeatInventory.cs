namespace API.Domain.Entities;

public class FlightSeatInventory
{
    public int Id { get; set; }
    public int FlightId { get; set; }
    public int SeatClassId { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int HeldSeats { get; set; }
    public int SoldSeats { get; set; }
    public decimal BasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Flight Flight { get; set; } = null!;
    public SeatClass SeatClass { get; set; } = null!;
    public ICollection<BookingPassenger> BookingPassengers { get; set; } = new List<BookingPassenger>();

    public bool CanBook(int count) => count > 0 && AvailableSeats >= count;

    public void ReserveSeats(int count)
    {
        if (!CanBook(count))
        {
            throw new InvalidOperationException("Insufficient available seats.");
        }

        AvailableSeats -= count;
        SoldSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void HoldSeats(int count)
    {
        if (!CanBook(count))
        {
            throw new InvalidOperationException("Insufficient available seats.");
        }

        AvailableSeats -= count;
        HeldSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseHold(int count)
    {
        if (count <= 0 || HeldSeats < count)
        {
            throw new InvalidOperationException("Invalid held seat release.");
        }

        HeldSeats -= count;
        AvailableSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetOccupancyPercent()
    {
        if (TotalSeats <= 0)
        {
            return 0;
        }

        return Math.Round((decimal)(SoldSeats + HeldSeats) * 100m / TotalSeats, 2);
    }

    public void UpdateDynamicPrice(decimal demandFactor, decimal timeFactor)
    {
        var factor = Math.Max(0.1m, demandFactor * timeFactor);
        CurrentPrice = Math.Round(BasePrice * factor, 2);
        UpdatedAt = DateTime.UtcNow;
    }
}
