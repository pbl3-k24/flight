using API.Domain.Enums;

namespace API.Domain.Entities;

public class Passenger
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string PassportNumber { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public SeatClass SeatClass { get; set; } = SeatClass.Economy;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Booking? Booking { get; set; }

    public bool IsAtLeastYearsOld(int age, DateTime asOfUtc)
    {
        var asOfDate = DateOnly.FromDateTime(asOfUtc);
        var years = asOfDate.Year - DateOfBirth.Year;

        if (DateOfBirth > asOfDate.AddYears(-years))
        {
            years--;
        }

        return years >= age;
    }

    public void ValidateBasicInfo()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            throw new InvalidOperationException("FirstName is required.");
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            throw new InvalidOperationException("LastName is required.");
        }

        if (DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("DateOfBirth cannot be in the future.");
        }

        if (string.IsNullOrWhiteSpace(PassportNumber))
        {
            throw new InvalidOperationException("PassportNumber is required.");
        }
    }
}
