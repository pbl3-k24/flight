using FlightBooking.Application.DTOs.Auth;

namespace FlightBooking.Application.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<IEnumerable<UserDto>> GetAllAsync(int page, int pageSize);
    Task SuspendUserAsync(Guid userId, string reason, Guid adminId);
    Task ActivateUserAsync(Guid userId, Guid adminId);
    Task DeleteUserAsync(Guid userId, Guid adminId);
}
