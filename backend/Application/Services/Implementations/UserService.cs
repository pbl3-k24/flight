using FlightBooking.Application.DTOs.Auth;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class UserService(IUserRepository userRepository, IAuditLogService auditLogService) : IUserService
{
    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdWithProfileAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");
        return MapToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await userRepository.GetByIdWithProfileAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.Profile is null)
        {
            user.Profile = new UserProfile { Id = Guid.NewGuid(), UserId = userId };
        }

        var before = user.Profile;
        user.Profile.FullName = request.FullName;
        user.Profile.DateOfBirth = request.DateOfBirth;
        user.Profile.Gender = request.Gender;
        user.Profile.Nationality = request.Nationality;
        user.Profile.IdentityNumber = request.IdentityNumber;
        user.Profile.PassportNumber = request.PassportNumber;
        user.Profile.UpdatedAt = DateTime.UtcNow;
        user.Phone = request.Phone ?? user.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.SaveChangesAsync();

        await auditLogService.LogAsync("profile_updated", "User", userId.ToString(), before, user.Profile, userId);
        return MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync(int page, int pageSize)
    {
        var users = await userRepository.GetAllWithProfileAsync(page, pageSize);
        return users.Select(MapToDto);
    }

    public async Task SuspendUserAsync(Guid userId, string reason, Guid adminId)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");
        var before = user.Status;
        user.Status = "suspended";
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync();
        await auditLogService.LogAsync("user_suspended", "User", userId.ToString(), new { Status = before, Reason = reason }, new { Status = "suspended" }, adminId);
    }

    public async Task ActivateUserAsync(Guid userId, Guid adminId)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");
        var before = user.Status;
        user.Status = "active";
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync();
        await auditLogService.LogAsync("user_activated", "User", userId.ToString(), new { Status = before }, new { Status = "active" }, adminId);
    }

    public async Task DeleteUserAsync(Guid userId, Guid adminId)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");
        user.DeletedAt = DateTime.UtcNow;
        user.Status = "deleted";
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync();
        await auditLogService.LogAsync("user_deleted", "User", userId.ToString(), null, new { DeletedAt = user.DeletedAt }, adminId);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id, user.Email, user.Phone, user.Status, user.EmailVerified,
            user.Profile is null ? null : new UserProfileDto(
                user.Profile.FullName, user.Profile.DateOfBirth, user.Profile.Gender,
                user.Profile.Nationality, user.Profile.IdentityNumber, user.Profile.PassportNumber, user.Profile.AvatarUrl),
            user.UserRoles.Select(ur => ur.Role?.Name ?? string.Empty));
    }
}
