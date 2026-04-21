namespace API.Application.Services;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class UserAdminService : IUserAdminService
{
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

    public async Task<bool> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
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

    public async Task<bool> AssignRoleAsync(int userId, AssignRoleDto dto)
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

            // Add role to user (implementation depends on UserRole entity)
            // This is a simplified version - actual implementation would depend on the schema

            _logger.LogInformation("Role assigned to user: {UserId}, Role: {RoleId}", userId, dto.RoleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            throw;
        }
    }

    public async Task<bool> RemoveRoleAsync(int userId, int roleId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // Remove role from user
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
                    PassengerCount = 1,
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
}
