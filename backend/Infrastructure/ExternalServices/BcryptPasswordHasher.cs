using FlightBooking.Domain.Interfaces.Services;

namespace FlightBooking.Infrastructure.ExternalServices;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => BCrypt.Net.BCrypt.EnhancedHashPassword(password, 12);

    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
}
