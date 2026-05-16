namespace API.Controllers.Admin;

using API.Application.Dtos.AdditionalService;
using API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/additional-services")]
[Authorize(Roles = "Admin")]
public class AdditionalServicesAdminController : ControllerBase
{
    private readonly IAdditionalServiceService _service;

    public AdditionalServicesAdminController(IAdditionalServiceService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<AdditionalServiceDto>> Create([FromBody] CreateAdditionalServiceDto dto)
    {
        var result = await _service.CreateServiceAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdditionalServiceDto>> Update(int id, [FromBody] UpdateAdditionalServiceDto dto)
    {
        var result = await _service.UpdateServiceAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await _service.DeleteServiceAsync(id);
        return NoContent();
    }

    [HttpPut("class-config/{seatClassId}")]
    public async Task<ActionResult> UpdateClassConfig(int seatClassId, [FromBody] SeatClassServiceConfigRequestDto request)
    {
        await _service.UpdateClassServiceConfigsAsync(seatClassId, request);
        return NoContent();
    }
}
