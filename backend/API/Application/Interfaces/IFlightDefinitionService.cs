namespace API.Application.Interfaces;

using API.Application.Dtos.FlightDefinition;

public interface IFlightDefinitionService
{
    Task<List<FlightDefinitionDto>> GetAllAsync();
    Task<List<FlightDefinitionDto>> GetActiveAsync();
    Task<FlightDefinitionDto?> GetByIdAsync(int id);
    Task<FlightDefinitionDto?> GetByFlightNumberAsync(string flightNumber);
    Task<FlightDefinitionDto> CreateAsync(CreateFlightDefinitionDto dto);
    Task<FlightDefinitionDto> UpdateAsync(int id, UpdateFlightDefinitionDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ActivateAsync(int id);
    Task<bool> DeactivateAsync(int id);
}
