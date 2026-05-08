namespace API.Application.Services;

using API.Application.Dtos.AdditionalService;

public interface IAdditionalServiceService
{
    Task<List<AdditionalServiceDto>> GetAllServicesAsync();
    Task<SeatClassServiceConfigDto> GetServicesBySeatClassAsync(int seatClassId);
}
