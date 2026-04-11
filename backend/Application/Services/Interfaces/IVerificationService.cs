namespace FlightBooking.Application.Services.Interfaces;

public interface IVerificationService
{
    Task SendEmailOtpAsync(Guid userId, string purpose);
    Task<bool> VerifyEmailOtpAsync(Guid userId, string code, string purpose);
    Task<bool> IsEmailVerifiedAsync(Guid userId);
}
