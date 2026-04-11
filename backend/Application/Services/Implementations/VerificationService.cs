using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class VerificationService(IUserRepository userRepository, IOtpTokenRepository otpTokenRepository, IEmailService emailService) : IVerificationService
{
    private const int OtpExpiryMinutes = 10;
    private const int MaxRetries = 5;

    public async Task SendEmailOtpAsync(Guid userId, string purpose)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        // Invalidate existing OTPs for this user+purpose
        await otpTokenRepository.InvalidateExistingAsync(userId, purpose);

        var code = GenerateCode();
        var otp = new OtpToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = code,
            Purpose = purpose,
            IsUsed = false,
            RetryCount = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        };
        await otpTokenRepository.AddAsync(otp);
        await otpTokenRepository.SaveChangesAsync();

        var templateKey = purpose switch
        {
            "email_verification" => "otp_email_verification",
            "password_reset" => "otp_password_reset",
            _ => "otp_generic"
        };

        await emailService.SendAsync(user.Email, templateKey, new { Code = code, ExpiryMinutes = OtpExpiryMinutes });
    }

    public async Task<bool> VerifyEmailOtpAsync(Guid userId, string code, string purpose)
    {
        var otp = await otpTokenRepository.GetActiveAsync(userId, purpose);
        if (otp is null) return false;

        if (otp.RetryCount >= MaxRetries)
        {
            otp.IsUsed = true;
            await otpTokenRepository.SaveChangesAsync();
            throw new InvalidOperationException("OTP has been locked due to too many failed attempts. Please request a new code.");
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            otp.IsUsed = true;
            await otpTokenRepository.SaveChangesAsync();
            return false;
        }

        if (otp.Code != code)
        {
            otp.RetryCount++;
            await otpTokenRepository.SaveChangesAsync();
            return false;
        }

        otp.IsUsed = true;
        await otpTokenRepository.SaveChangesAsync();

        if (purpose == "email_verification")
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user is not null)
            {
                user.EmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await userRepository.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> IsEmailVerifiedAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        return user?.EmailVerified ?? false;
    }

    private static string GenerateCode()
        => Random.Shared.Next(100000, 999999).ToString();
}
