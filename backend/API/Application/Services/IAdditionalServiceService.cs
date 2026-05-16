namespace API.Application.Services;

using API.Application.Dtos.AdditionalService;

public interface IAdditionalServiceService
{
    Task<List<AdditionalServiceDto>> GetAllServicesAsync();
    Task<SeatClassServiceConfigDto> GetServicesBySeatClassAsync(int seatClassId);
    
    // Admin operations
    Task<AdditionalServiceDto> CreateServiceAsync(CreateAdditionalServiceDto dto);
    Task<AdditionalServiceDto> UpdateServiceAsync(int id, UpdateAdditionalServiceDto dto);
    Task<bool> DeleteServiceAsync(int id);
    Task<bool> UpdateClassServiceConfigsAsync(int seatClassId, SeatClassServiceConfigRequestDto request);
}
