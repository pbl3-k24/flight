namespace API.Infrastructure.Services;

using API.Application.Interfaces;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

/// <summary>
/// Utility for generating cryptographically secure booking codes
/// </summary>
public class BookingCodeGenerator
{
    private const string ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 10;
    private const int MaxRetries = 5;

    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingCodeGenerator> _logger;

    public BookingCodeGenerator(IBookingRepository bookingRepository, ILogger<BookingCodeGenerator> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generates a unique, cryptographically secure booking code
    /// </summary>
    public async Task<string> GenerateUniqueCodeAsync()
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var code = GenerateCryptoRandomCode();

            // Verify uniqueness - use Any() for EF Core compatibility
            var bookings = await _bookingRepository.GetAllAsync();
            var exists = bookings.Any(b => b.BookingCode == code);

            if (!exists)
            {
                _logger.LogDebug("Generated unique booking code (attempt {Attempt})", attempt + 1);
                return code;
            }

            _logger.LogWarning("Booking code collision detected (attempt {Attempt})", attempt + 1);
        }

        throw new InvalidOperationException(
            $"Failed to generate unique booking code after {MaxRetries} retries. " +
            "System may be under high load. Please retry the request.");
    }

    /// <summary>
    /// Generates a cryptographically secure random code
    /// </summary>
    private static string GenerateCryptoRandomCode()
    {
        var result = new char[CodeLength];

        using (var rng = new RNGCryptoServiceProvider())
        {
            var data = new byte[CodeLength];
            rng.GetBytes(data);

            for (int i = 0; i < CodeLength; i++)
            {
                result[i] = ValidChars[data[i] % ValidChars.Length];
            }
        }

        return new string(result);
    }
}
