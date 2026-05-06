namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IFlightTemplateDetailRepository
{
    Task<FlightTemplateDetail?> GetByIdAsync(int id);
    
    Task<IEnumerable<FlightTemplateDetail>> GetByTemplateIdAsync(int templateId);
    
    Task<FlightTemplateDetail> CreateAsync(FlightTemplateDetail detail);
    
    Task UpdateAsync(FlightTemplateDetail detail);
    
    Task DeleteAsync(int id);
    
    Task DeleteByTemplateIdAsync(int templateId);
}
