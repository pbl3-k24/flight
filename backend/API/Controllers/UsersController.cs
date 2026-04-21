namespace API.Controllers;

using API.Application.Dtos.Auth;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IAuthService authService,
        ILogger<UsersController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// Sends verification email with code to user's email address.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RegisterAsync([FromBody] RegisterDto dto)
    {
        _logger.LogInformation("User registration attempt");
        var response = await _authService.RegisterAsync(dto);
        return Created($"api/v1/users/{response.UserId}", new { success = true, data = response });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> LoginAsync([FromBody] LoginDto dto)
    {
        _logger.LogInformation("Login attempt");
        var response = await _authService.LoginAsync(dto);
        return Ok(response);
    }

    /// <summary>
    /// Verifies user email with the provided verification code.
    /// No authentication required - code is enough to verify.
    /// Flow: User registers → Receives email with code → Clicks /verify-email?code=abc123
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmailAsync([FromQuery] string code)
    {
        _logger.LogInformation("Email verification request received");
        await _authService.VerifyEmailAsync(null!, code);
        return Ok(new { message = "Email verified successfully" });
    }

    /// <summary>
    /// Changes the password for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto dto)
    {
        var userId = User.GetUserIdOrThrow();
        _logger.LogInformation("Password change request for authenticated user");
        await _authService.ChangePasswordAsync(userId, dto);
        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Requests a password reset for the given email.
    /// Returns success regardless of whether account exists (prevents email enumeration).
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordDto dto)
    {
        _logger.LogInformation("Password reset request");
        await _authService.RequestPasswordResetAsync(dto.Email);
        // Always return success to prevent email enumeration attacks
        return Ok(new { message = "If an account with that email exists, a password reset link has been sent" });
    }

    /// <summary>
    /// Resets the password using a reset code.
    /// No authentication required - code is enough to reset.
    /// Flow: User requests reset → Receives email with code → Calls /reset-password with code + new password
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto dto)
    {
        _logger.LogInformation("Password reset request with code");
        await _authService.ResetPasswordAsync(dto.Code, dto.NewPassword);
        return Ok(new { message = "Password reset successfully" });
    }
}
