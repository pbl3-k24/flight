namespace API.Application.Interfaces;

using API.Application.Dtos.Admin;

public interface IUserAdminService
{
    /// <summary>
    /// Gets all users with optional filters.
    /// </summary>
    Task<List<UserManagementResponse>> GetUsersAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets user details.
    /// </summary>
    Task<UserManagementResponse> GetUserAsync(int userId);

    /// <summary>
    /// Updates user status (Active/Inactive/Suspended).
    /// </summary>
    Task<bool> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task<bool> AssignRoleAsync(int userId, AssignRoleDto dto);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<bool> RemoveRoleAsync(int userId, int roleId);

    /// <summary>
    /// Gets user booking history.
    /// </summary>
    Task<List<BookingManagementResponse>> GetUserBookingsAsync(int userId);
}
