namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class UserAdminService : IUserAdminService
{
    private const string AdminRoleName = "Admin";
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IBookingRepository bookingRepository,
        ILogger<UserAdminService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<List<UserManagementResponse>> GetUsersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var results = new List<UserManagementResponse>();

            foreach (var user in users.Skip((page - 1) * pageSize).Take(pageSize))
            {
                results.Add(await BuildUserResponseAsync(user));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            throw;
        }
    }

    public async Task<UserManagementResponse> GetUserAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            return await BuildUserResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            throw;
        }
    }

    public async Task<bool> UpdateUserStatusAsync(int actorAdminId, int userId, UpdateUserStatusDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            var adminRole = await _roleRepository.GetByNameAsync(AdminRoleName)
                ?? throw new NotFoundException("Admin role not found");
            var founderAdminId = await GetFounderAdminIdAsync(adminRole.Id);

            var targetIsAdmin = await _userRepository.UserHasRoleAsync(userId, adminRole.Id);
            var isDeactivating = dto.Status != 0;
            if (targetIsAdmin && isDeactivating && actorAdminId != founderAdminId)
            {
                throw new UnauthorizedException("Only the first admin can deactivate admin accounts");
            }

            if (userId == founderAdminId && isDeactivating)
            {
                throw new ValidationException("Cannot deactivate the first admin account");
            }

            user.Status = dto.Status;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User status updated: {UserId}, Status: {Status}", userId, dto.Status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status");
            throw;
        }
    }

    public async Task<bool> AssignRoleAsync(int actorAdminId, int userId, AssignRoleDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            var role = await _roleRepository.GetByIdAsync(dto.RoleId);
            if (role == null)
            {
                throw new NotFoundException("Role not found");
            }

            if (role.Name.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase))
            {
                var founderAdminId = await GetFounderAdminIdAsync(role.Id);
                if (actorAdminId != founderAdminId)
                {
                    throw new UnauthorizedException("Only the first admin can assign Admin role");
                }
            }

            await _userRepository.AddRoleAsync(userId, dto.RoleId);

            _logger.LogInformation("Role assigned to user: {UserId}, Role: {RoleId}", userId, dto.RoleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            throw;
        }
    }

    public async Task<bool> RemoveRoleAsync(int actorAdminId, int userId, int roleId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                throw new NotFoundException("Role not found");
            }

            if (role.Name.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase))
            {
                var founderAdminId = await GetFounderAdminIdAsync(roleId);
                if (actorAdminId != founderAdminId)
                {
                    throw new UnauthorizedException("Only the first admin can remove Admin role");
                }

                if (userId == founderAdminId)
                {
                    throw new ValidationException("Cannot remove Admin role from the first admin");
                }
            }

            await _userRepository.RemoveRoleAsync(userId, roleId);
            _logger.LogInformation("Role removed from user: {UserId}, Role: {RoleId}", userId, roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            throw;
        }
    }

    public async Task<List<BookingManagementResponse>> GetUserBookingsAsync(int userId)
    {
        try
        {
            var bookings = await _bookingRepository.GetByUserIdAsync(userId, 1, 100);
            var results = new List<BookingManagementResponse>();

            foreach (var booking in bookings)
            {
                var user = await _userRepository.GetByIdAsync(booking.UserId);
                var statusName = booking.Status switch
                {
                    0 => "Pending",
                    1 => "Confirmed",
                    2 => "CheckedIn",
                    3 => "Cancelled",
                    _ => "Unknown"
                };

                results.Add(new BookingManagementResponse
                {
                    BookingId = booking.Id,
                    BookingCode = booking.BookingCode,
                    UserEmail = user?.Email ?? "Unknown",
                    UserName = user?.FullName ?? "Unknown",
                    PassengerCount = booking.Passengers?.Count ?? 0,
                    Amount = booking.FinalAmount,
                    BookingStatus = booking.Status,
                    BookingStatusName = statusName,
                    CreatedAt = booking.CreatedAt
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user bookings");
            throw;
        }
    }

    private async Task<UserManagementResponse> BuildUserResponseAsync(User user)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(user.Id, 1, 1000);
        var totalSpent = bookings.Sum(b => b.FinalAmount);

        var statusName = user.Status switch
        {
            0 => "Active",
            1 => "Inactive",
            2 => "Suspended",
            _ => "Unknown"
        };

        return new UserManagementResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Status = user.Status,
            StatusName = statusName,
            Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? [],
            BookingCount = bookings.Count,
            TotalSpent = totalSpent,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task<int> GetFounderAdminIdAsync(int adminRoleId)
    {
        var users = await _userRepository.GetAllWithRolesAsync();
        var founderAdmin = users
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == adminRoleId))
            .OrderBy(u => u.Id)
            .FirstOrDefault();

        if (founderAdmin == null)
        {
            throw new ValidationException("No admin account exists in the system");
        }

        return founderAdmin.Id;
    }
}
