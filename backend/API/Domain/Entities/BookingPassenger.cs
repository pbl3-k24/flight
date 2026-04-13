using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace API.Domain.Entities;

public class BookingPassenger
{
    public int Id { get; set; }
    public int BookingId { get; set; }

    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(50)]
    public string? NationalId { get; set; }

    public int PassengerType { get; set; }
    public int FlightSeatInventoryId { get; set; }
    public string? FareSnapshot { get; set; }

    public Booking Booking { get; set; } = null!;
    public FlightSeatInventory FlightSeatInventory { get; set; } = null!;
    public ICollection<BookingService> Services { get; set; } = new List<BookingService>();
    public Ticket? Ticket { get; set; }

    public bool IsAdult() => PassengerType == 0;

    public bool IsChild() => PassengerType == 1;

    public bool IsInfant() => PassengerType == 2;

    public int GetAge()
    {
        if (!DateOfBirth.HasValue)
        {
            return 0;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - DateOfBirth.Value.Year;
        if (DateOfBirth.Value > today.AddYears(-age))
        {
            age--;
        }

        return Math.Max(age, 0);
    }

    public object? GetFareDetails()
    {
        if (string.IsNullOrWhiteSpace(FareSnapshot))
        {
            return null;
        }

        return JsonSerializer.Deserialize<object>(FareSnapshot);
    }
}
