namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IFlightScheduleTemplateRepository
{
    Task<FlightScheduleTemplate?> GetByIdAsync(int id);
    
    Task<FlightScheduleTemplate?> GetByIdWithDetailsAsync(int id);
    
    Task<IEnumerable<FlightScheduleTemplate>> GetAllAsync();
    
    Task<IEnumerable<FlightScheduleTemplate>> GetAllWithDetailsAsync();
    
    Task<FlightScheduleTemplate> CreateAsync(FlightScheduleTemplate template);
    
    Task UpdateAsync(FlightScheduleTemplate template);
    
    Task DeleteAsync(int id);
}
