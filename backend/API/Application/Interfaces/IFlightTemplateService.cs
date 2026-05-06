namespace API.Application.Interfaces;

using API.Application.Dtos.FlightTemplate;

/// <summary>
/// Service for managing flight schedule templates and generating flights from templates
/// </summary>
public interface IFlightTemplateService
{
    // ========== CRUD Operations for Templates ==========
    
    /// <summary>
    /// Get all flight schedule templates
    /// </summary>
    Task<List<FlightScheduleTemplateDto>> GetAllTemplatesAsync();
    
    /// <summary>
    /// Get template by ID with details
    /// </summary>
    Task<FlightScheduleTemplateDto?> GetTemplateByIdAsync(int templateId);
    
    /// <summary>
    /// Create a new flight schedule template (save to DB for reuse)
    /// </summary>
    Task<FlightScheduleTemplateDto> CreateTemplateAsync(CreateFlightTemplateDto dto);
    
    /// <summary>
    /// Update existing template
    /// </summary>
    Task<FlightScheduleTemplateDto> UpdateTemplateAsync(int templateId, CreateFlightTemplateDto dto);
    
    /// <summary>
    /// Delete template
    /// </summary>
    Task<bool> DeleteTemplateAsync(int templateId);
    
    // ========== Generate Flights from Template ==========
    
    /// <summary>
    /// Generate actual flights from a saved template for specified week(s)
    /// This is the main function that creates real Flight records in DB
    /// </summary>
    Task<GenerateFlightsResultDto> GenerateFlightsFromTemplateAsync(GenerateFlightsFromTemplateDto dto);
}
