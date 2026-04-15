namespace API.Application.Dtos.Auth;

public class ResetPasswordDto
{
    public required string Code { get; set; }

    public required string NewPassword { get; set; }
}
