using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IAircraftService
{
    Task<AircraftDto> GetByIdAsync(Guid id);
    Task<IEnumerable<AircraftDto>> GetAllAsync();
    Task<AircraftDto> CreateAsync(CreateAircraftRequest request, Guid adminId);
    Task<AircraftDto> UpdateAsync(Guid id, UpdateAircraftRequest request, Guid adminId);
    Task DeactivateAsync(Guid id, Guid adminId);
}
