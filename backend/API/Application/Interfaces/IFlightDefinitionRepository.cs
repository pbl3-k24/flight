namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IFlightDefinitionRepository
{
    Task<FlightDefinition?> GetByIdAsync(int id);
    
    Task<FlightDefinition?> GetByFlightNumberAsync(string flightNumber);
    
    Task<IEnumerable<FlightDefinition>> GetAllAsync();
    
    Task<IEnumerable<FlightDefinition>> GetActiveAsync();
    
    Task<FlightDefinition> CreateAsync(FlightDefinition flightDefinition);
    
    Task UpdateAsync(FlightDefinition flightDefinition);
    
    Task DeleteAsync(int id);
    
    /// <summary>
    /// Find or create a FlightDefinition based on flight pattern
    /// Used by FlightTemplateService to ensure FlightDefinition exists before creating Flight
    /// </summary>
    Task<FlightDefinition> FindOrCreateAsync(
        string flightNumber,
        int routeId,
        int defaultAircraftId,
        TimeOnly departureTime,
        TimeOnly arrivalTime,
        int arrivalOffsetDays = 0,
        int operatingDays = 127);
}
