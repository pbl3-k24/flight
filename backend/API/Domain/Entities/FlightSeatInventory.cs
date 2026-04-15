namespace API.Domain.Entities;

public class FlightSeatInventory
{
    public int Id { get; set; }

    public int FlightId { get; set; }

    public int SeatClassId { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public int HeldSeats { get; set; } = 0;

    public int SoldSeats { get; set; } = 0;

    public decimal BasePrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public int Version { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual Flight Flight { get; set; } = null!;

    public virtual SeatClass SeatClass { get; set; } = null!;

    public virtual ICollection<BookingPassenger> BookingPassengers { get; set; } = [];

    // Domain methods
    public bool CanBook(int count) => AvailableSeats >= count;

    public void ReserveSeats(int count)
    {
        if (!CanBook(count))
            throw new InvalidOperationException($"Cannot reserve {count} seats. Available: {AvailableSeats}");

        AvailableSeats -= count;
        SoldSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void HoldSeats(int count)
    {
        if (AvailableSeats < count)
            throw new InvalidOperationException($"Cannot hold {count} seats. Available: {AvailableSeats}");

        AvailableSeats -= count;
        HeldSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseHold(int count)
    {
        if (HeldSeats < count)
            throw new InvalidOperationException($"Cannot release {count} held seats. Held: {HeldSeats}");

        HeldSeats -= count;
        AvailableSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetOccupancyPercent()
    {
        if (TotalSeats == 0)
            return 0;

        return ((SoldSeats + HeldSeats) / (decimal)TotalSeats) * 100;
    }

    public void UpdateDynamicPrice(decimal demandFactor, decimal timeFactor)
    {
        CurrentPrice = BasePrice * demandFactor * timeFactor;
        UpdatedAt = DateTime.UtcNow;
    }
}
