using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface ISeatTemplateService
{
    Task<IEnumerable<SeatTemplateDto>> GetByAircraftAsync(Guid aircraftId);
    Task<SeatTemplateDto> CreateAsync(CreateSeatTemplateRequest request, Guid adminId);
    Task BulkCreateAsync(Guid aircraftId, IEnumerable<CreateSeatTemplateRequest> requests, Guid adminId);
    Task DeleteAsync(Guid id, Guid adminId);
}
