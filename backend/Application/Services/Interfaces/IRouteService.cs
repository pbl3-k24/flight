using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IRouteService
{
    Task<RouteDto> GetByIdAsync(Guid id);
    Task<IEnumerable<RouteDto>> GetAllAsync();
    Task<RouteDto> CreateAsync(CreateRouteRequest request, Guid adminId);
    Task<RouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, Guid adminId);
    Task DeactivateAsync(Guid id, Guid adminId);
}
