namespace FlightBooking.Application.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string FullName, string? Phone);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);

public record ResetPasswordRequest(string Email, string OtpCode, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateProfileRequest(
    string FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Nationality,
    string? IdentityNumber,
    string? PassportNumber,
    string? Phone);

public record UserDto(
    Guid Id,
    string Email,
    string? Phone,
    string Status,
    bool EmailVerified,
    UserProfileDto? Profile,
    IEnumerable<string> Roles);

public record UserProfileDto(
    string FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Nationality,
    string? IdentityNumber,
    string? PassportNumber,
    string? AvatarUrl);
