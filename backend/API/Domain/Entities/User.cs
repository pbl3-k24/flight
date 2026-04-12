using API.Domain.Enums;

namespace API.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string Nationality { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];

    public bool IsAdult(DateTime asOfUtc)
    {
        var asOfDate = DateOnly.FromDateTime(asOfUtc);
        var years = asOfDate.Year - DateOfBirth.Year;

        if (DateOfBirth > asOfDate.AddYears(-years))
        {
            years--;
        }

        return years >= 18;
    }

    public bool CanMakeBooking(DateTime asOfUtc)
    {
        return Status == UserStatus.Active && IsAdult(asOfUtc);
    }

    public void UpdateProfile(string firstName, string lastName, string phoneNumber, string nationality)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber.Trim();
        Nationality = nationality.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Deleted users cannot be suspended.");
        }

        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Deleted users cannot be reactivated.");
        }

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDeleted()
    {
        Status = UserStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            throw new InvalidOperationException("Email must be a valid format.");
        }

        if (string.IsNullOrWhiteSpace(PasswordHash))
        {
            throw new InvalidOperationException("PasswordHash is required.");
        }

        if (DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("DateOfBirth cannot be in the future.");
        }
    }
}
