namespace API.Application.Dtos.Auth;

public class AuthResponse
{
    public int UserId { get; set; }

    public required string Email { get; set; }

    public required string FullName { get; set; }

    public required string Token { get; set; }

    public DateTime ExpiresAt { get; set; }
}
