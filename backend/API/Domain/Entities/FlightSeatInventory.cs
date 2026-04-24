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

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Flight Flight { get; set; } = null!;

    public virtual SeatClass SeatClass { get; set; } = null!;

    public virtual ICollection<BookingPassenger> BookingPassengers { get; set; } = [];

    // Domain methods for seat state machine
    // State invariant: Available + Held + Sold <= Total (always holds)

    public bool CanHold(int count) => AvailableSeats >= count;

    /// <summary>
    /// Hold seats when booking is created (Available -> Held)
    /// Called in: BookingService.CreateBookingAsync()
    /// </summary>
    public void HoldSeats(int count)
    {
        if (AvailableSeats < count)
            throw new InvalidOperationException($"Cannot hold {count} seats. Available: {AvailableSeats}");

        AvailableSeats -= count;
        HeldSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Convert held seats to sold when payment succeeds (Held -> Sold)
    /// Called in: PaymentService.ProcessPaymentAsync()
    /// </summary>
    public void ConfirmHeldSeats(int count)
    {
        if (HeldSeats < count)
            throw new InvalidOperationException($"Cannot confirm {count} held seats. Held: {HeldSeats}");

        HeldSeats -= count;
        SoldSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Release held seats back to available (Held -> Available)
    /// Called in: PaymentService when payment fails/expires, BookingService when payment times out
    /// </summary>
    public void ReleaseHeldSeats(int count)
    {
        if (HeldSeats < count)
            throw new InvalidOperationException($"Cannot release {count} held seats. Held: {HeldSeats}");

        HeldSeats -= count;
        AvailableSeats += count;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Release sold seats back to available (Sold -> Available)
    /// Called in: BookingService.CancelBookingAsync() when cancelling confirmed booking
    /// </summary>
    public void CancelSoldSeats(int count)
    {
        if (SoldSeats < count)
            throw new InvalidOperationException($"Cannot cancel {count} sold seats. Sold: {SoldSeats}");

        SoldSeats -= count;
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

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
