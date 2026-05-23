namespace API.Application.Services;

using API.Application.Dtos.Passenger;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class SavedPassengerService : ISavedPassengerService
{
    private readonly ISavedPassengerRepository _savedPassengerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SavedPassengerService> _logger;

    public SavedPassengerService(
        ISavedPassengerRepository savedPassengerRepository,
        IUserRepository userRepository,
        ILogger<SavedPassengerService> logger)
    {
        _savedPassengerRepository = savedPassengerRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<List<SavedPassengerResponse>> GetMyPassengersAsync(int userId)
    {
        var user = await RequireUserAsync(userId);
        await EnsureDefaultPassengerSyncedAsync(user);

        var passengers = await _savedPassengerRepository.GetByUserIdAsync(userId);
        return passengers.Select(MapResponse).ToList();
    }

    public async Task<SavedPassengerResponse> CreateAsync(int userId, CreateSavedPassengerDto dto)
    {
        await RequireUserAsync(userId);
        Validate(dto.FirstName, dto.LastName, dto.Email);

        var passenger = new SavedPassenger
        {
            UserId = userId,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = NormalizeNullable(dto.Gender),
            Nationality = NormalizeNullable(dto.Nationality),
            DocumentNumber = NormalizeNullable(dto.DocumentNumber),
            Email = dto.Email.Trim(),
            Phone = NormalizeNullable(dto.Phone),
            IsDefaultOwner = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _savedPassengerRepository.CreateAsync(passenger);
        return MapResponse(created);
    }

    public async Task<SavedPassengerResponse> UpdateAsync(int userId, int passengerId, UpdateSavedPassengerDto dto)
    {
        Validate(dto.FirstName, dto.LastName, dto.Email);

        var passenger = await _savedPassengerRepository.GetByIdAsync(passengerId);
        if (passenger == null || passenger.UserId != userId)
        {
            throw new NotFoundException("Saved passenger not found");
        }

        passenger.FirstName = dto.FirstName.Trim();
        passenger.LastName = dto.LastName.Trim();
        passenger.DateOfBirth = dto.DateOfBirth;
        passenger.Gender = NormalizeNullable(dto.Gender);
        passenger.Nationality = NormalizeNullable(dto.Nationality);
        passenger.DocumentNumber = NormalizeNullable(dto.DocumentNumber);
        passenger.Email = dto.Email.Trim();
        passenger.Phone = NormalizeNullable(dto.Phone);
        passenger.UpdatedAt = DateTime.UtcNow;

        await _savedPassengerRepository.UpdateAsync(passenger);
        return MapResponse(passenger);
    }

    public async Task<bool> DeleteAsync(int userId, int passengerId)
    {
        var passenger = await _savedPassengerRepository.GetByIdAsync(passengerId);
        if (passenger == null || passenger.UserId != userId)
        {
            throw new NotFoundException("Saved passenger not found");
        }

        if (passenger.IsDefaultOwner)
        {
            throw new ValidationException("Default owner passenger cannot be deleted");
        }

        passenger.IsDeleted = true;
        passenger.DeletedAt = DateTime.UtcNow;
        passenger.UpdatedAt = DateTime.UtcNow;
        await _savedPassengerRepository.UpdateAsync(passenger);
        return true;
    }

    private async Task<User> RequireUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        return user;
    }

    private async Task EnsureDefaultPassengerSyncedAsync(User user)
    {
        var (firstName, lastName) = SplitName(user.FullName);
        var defaultPassenger = await _savedPassengerRepository.GetDefaultByUserIdAsync(user.Id);

        if (defaultPassenger == null)
        {
            var created = new SavedPassenger
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = user.DateOfBirth,
                Gender = NormalizeNullable(user.Gender),
                Nationality = NormalizeNullable(user.Nationality),
                DocumentNumber = null,
                Email = user.Email,
                Phone = NormalizeNullable(user.Phone),
                IsDefaultOwner = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _savedPassengerRepository.CreateAsync(created);
            _logger.LogInformation("Created default saved passenger for user {UserId}", user.Id);
            return;
        }

        var changed = false;
        if (!string.Equals(defaultPassenger.FirstName, firstName, StringComparison.Ordinal))
        {
            defaultPassenger.FirstName = firstName;
            changed = true;
        }

        if (!string.Equals(defaultPassenger.LastName, lastName, StringComparison.Ordinal))
        {
            defaultPassenger.LastName = lastName;
            changed = true;
        }

        if (defaultPassenger.DateOfBirth != user.DateOfBirth)
        {
            defaultPassenger.DateOfBirth = user.DateOfBirth;
            changed = true;
        }

        var normalizedGender = NormalizeNullable(user.Gender);
        if (!string.Equals(defaultPassenger.Gender, normalizedGender, StringComparison.Ordinal))
        {
            defaultPassenger.Gender = normalizedGender;
            changed = true;
        }

        var normalizedNationality = NormalizeNullable(user.Nationality);
        if (!string.Equals(defaultPassenger.Nationality, normalizedNationality, StringComparison.Ordinal))
        {
            defaultPassenger.Nationality = normalizedNationality;
            changed = true;
        }

        if (!string.Equals(defaultPassenger.Email, user.Email, StringComparison.Ordinal))
        {
            defaultPassenger.Email = user.Email;
            changed = true;
        }

        var normalizedPhone = NormalizeNullable(user.Phone);
        if (!string.Equals(defaultPassenger.Phone, normalizedPhone, StringComparison.Ordinal))
        {
            defaultPassenger.Phone = normalizedPhone;
            changed = true;
        }

        if (changed)
        {
            defaultPassenger.UpdatedAt = DateTime.UtcNow;
            await _savedPassengerRepository.UpdateAsync(defaultPassenger);
            _logger.LogInformation("Synced default saved passenger for user {UserId}", user.Id);
        }
    }

    private static SavedPassengerResponse MapResponse(SavedPassenger passenger)
    {
        return new SavedPassengerResponse
        {
            Id = passenger.Id,
            FirstName = passenger.FirstName,
            LastName = passenger.LastName,
            DateOfBirth = passenger.DateOfBirth,
            Gender = passenger.Gender,
            Nationality = passenger.Nationality,
            DocumentNumber = passenger.DocumentNumber,
            Email = passenger.Email,
            Phone = passenger.Phone,
            IsDefaultOwner = passenger.IsDefaultOwner,
            UpdatedAt = passenger.UpdatedAt
        };
    }

    private static void Validate(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ValidationException("First name is required");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ValidationException("Last name is required");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required");
        }
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var normalized = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ("User", "Owner");
        }

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], parts[0]);
        }

        var firstName = parts[0];
        var lastName = string.Join(' ', parts.Skip(1));
        return (firstName, lastName);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

