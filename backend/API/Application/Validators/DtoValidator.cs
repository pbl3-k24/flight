namespace API.Application.Validators;

using API.Application.Exceptions;
using API.Application.Dtos.Auth;
using API.Application.Dtos.Booking;
using System.Text.RegularExpressions;

public static class DtoValidator
{
    public static void ValidateRegisterDto(RegisterDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(dto.Email) || !IsValidEmail(dto.Email))
            errors["email"] = new[] { "Email is required and must be valid" };

        if (string.IsNullOrEmpty(dto.Password) || !IsValidPassword(dto.Password))
            errors["password"] = new[] { "Password must be at least 8 characters with uppercase, lowercase, digit, and special character" };

        if (string.IsNullOrEmpty(dto.FullName) || dto.FullName.Length < 2)
            errors["fullName"] = new[] { "Full name must be at least 2 characters" };

        if (!string.IsNullOrEmpty(dto.Phone) && !IsValidPhone(dto.Phone))
            errors["phone"] = new[] { "Phone must be a valid international number" };

        if (errors.Count > 0)
            throw new ValidationException("Validation failed", errors);
    }

    public static void ValidateLoginDto(LoginDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(dto.Email) || !IsValidEmail(dto.Email))
            errors["email"] = new[] { "Email is required and must be valid" };

        if (string.IsNullOrEmpty(dto.Password))
            errors["password"] = new[] { "Password is required" };

        if (errors.Count > 0)
            throw new ValidationException("Validation failed", errors);
    }

    public static void ValidateChangePasswordDto(ChangePasswordDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(dto.OldPassword))
            errors["oldPassword"] = new[] { "Current password is required" };

        if (string.IsNullOrEmpty(dto.NewPassword) || !IsValidPassword(dto.NewPassword))
            errors["newPassword"] = new[] { "New password must be at least 8 characters" };

        if (dto.OldPassword == dto.NewPassword)
            errors["newPassword"] = new[] { "New password must differ from old password" };

        if (errors.Count > 0)
            throw new ValidationException("Validation failed", errors);
    }

    public static void ValidateCreateBookingDto(CreateBookingDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.OutboundFlightId <= 0)
            errors["outboundFlightId"] = new[] { "Flight ID is required" };

        if (dto.PassengerCount <= 0 || dto.PassengerCount > 9)
            errors["passengerCount"] = new[] { "Between 1 and 9 passengers required" };

        if (dto.Passengers == null || dto.Passengers.Count == 0)
            errors["passengers"] = new[] { "At least one passenger required" };
        else if (dto.Passengers.Count != dto.PassengerCount)
            errors["passengers"] = new[] { "Passenger count mismatch" };

        if (errors.Count > 0)
            throw new ValidationException("Validation failed", errors);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        var hasUpper = Regex.IsMatch(password, @"[A-Z]");
        var hasLower = Regex.IsMatch(password, @"[a-z]");
        var hasDigit = Regex.IsMatch(password, @"[0-9]");
        var hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*]");

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static bool IsValidPhone(string phone)
    {
        return Regex.IsMatch(phone, @"^\+?[0-9]{10,15}$");
    }
}
