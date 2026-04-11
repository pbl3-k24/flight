using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IFlightService
{
    Task<FlightDto> GetByIdAsync(Guid id);
    Task<IEnumerable<FlightDto>> GetAllAsync(int page, int pageSize);
    Task<FlightDto> CreateAsync(CreateFlightRequest request, Guid adminId);
    Task<FlightDto> UpdateAsync(Guid id, UpdateFlightRequest request, Guid adminId);
    Task CancelFlightAsync(Guid id, string reason, Guid adminId);
    Task UpdateStatusAsync(Guid id, string status, Guid adminId);
}
