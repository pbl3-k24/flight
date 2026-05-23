namespace API.Application.Interfaces;

using API.Application.Dtos.Passenger;

public interface ISavedPassengerService
{
    Task<List<SavedPassengerResponse>> GetMyPassengersAsync(int userId);
    Task<SavedPassengerResponse> CreateAsync(int userId, CreateSavedPassengerDto dto);
    Task<SavedPassengerResponse> UpdateAsync(int userId, int passengerId, UpdateSavedPassengerDto dto);
    Task<bool> DeleteAsync(int userId, int passengerId);
}

