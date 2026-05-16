namespace API.Controllers;

using API.Application.Dtos.AdditionalService;
using API.Application.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/additional-services")]
public class AdditionalServicesController : ControllerBase
{
    private readonly IAdditionalServiceService _service;

    public AdditionalServicesController(IAdditionalServiceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdditionalServiceDto>>> GetAll()
    {
        var services = await _service.GetAllServicesAsync();
        return Ok(services);
    }

    [HttpGet("by-seat-class/{seatClassId}")]
    public async Task<ActionResult<SeatClassServiceConfigDto>> GetBySeatClass(int seatClassId)
    {
        var result = await _service.GetServicesBySeatClassAsync(seatClassId);
        return Ok(result);
    }
}
