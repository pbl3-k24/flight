namespace API.Controllers;

using API.Application.Dtos.Admin;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/admin/airports")]
[Authorize(Roles = "Admin")]
public class AirportsAdminController : ControllerBase
{
    private readonly IAirportRepository _airportRepository;
    private readonly ILogger<AirportsAdminController> _logger;

    public AirportsAdminController(
        IAirportRepository airportRepository,
        ILogger<AirportsAdminController> logger)
    {
        _airportRepository = airportRepository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AirportManagementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false)
    {
        var airports = await _airportRepository.GetAllAsync();
        var filtered = airports
            .Where(a => includeDeleted || !a.IsDeleted)
            .OrderBy(a => a.Code)
            .Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
            .Take(Math.Max(1, pageSize))
            .Select(Map)
            .ToList();

        return Ok(filtered);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AirportManagementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var airport = await _airportRepository.GetByIdAsync(id);
        if (airport == null || airport.IsDeleted)
        {
            return NotFound(new { message = $"Airport {id} not found" });
        }

        return Ok(Map(airport));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AirportManagementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAirportDto dto)
    {
        try
        {
            ValidateRequired(dto.Code, nameof(dto.Code));
            ValidateRequired(dto.Name, nameof(dto.Name));
            ValidateRequired(dto.City, nameof(dto.City));

            var normalizedCode = dto.Code.Trim().ToUpperInvariant();
            var existing = await _airportRepository.GetByCodeAsync(normalizedCode);
            if (existing != null && !existing.IsDeleted)
            {
                throw new ValidationException($"Airport code '{normalizedCode}' already exists.");
            }

            var airport = new Airport
            {
                Code = normalizedCode,
                Name = dto.Name.Trim(),
                City = dto.City.Trim(),
                Province = string.IsNullOrWhiteSpace(dto.Province) ? null : dto.Province.Trim(),
                IsActive = dto.IsActive,
                IsDeleted = false
            };

            var created = await _airportRepository.CreateAsync(airport);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating airport");
            return StatusCode(500, new { message = "Error creating airport" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AirportManagementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAirportDto dto)
    {
        try
        {
            var airport = await _airportRepository.GetByIdAsync(id);
            if (airport == null || airport.IsDeleted)
            {
                return NotFound(new { message = $"Airport {id} not found" });
            }

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var normalizedCode = dto.Code.Trim().ToUpperInvariant();
                var existing = await _airportRepository.GetByCodeAsync(normalizedCode);
                if (existing != null && existing.Id != id && !existing.IsDeleted)
                {
                    throw new ValidationException($"Airport code '{normalizedCode}' already exists.");
                }

                airport.Code = normalizedCode;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                airport.Name = dto.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.City))
            {
                airport.City = dto.City.Trim();
            }

            if (dto.Province != null)
            {
                airport.Province = string.IsNullOrWhiteSpace(dto.Province) ? null : dto.Province.Trim();
            }

            if (dto.IsActive.HasValue)
            {
                airport.IsActive = dto.IsActive.Value;
            }

            await _airportRepository.UpdateAsync(airport);
            return Ok(Map(airport));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating airport {AirportId}", id);
            return StatusCode(500, new { message = "Error updating airport" });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var airport = await _airportRepository.GetByIdAsync(id);
        if (airport == null || airport.IsDeleted)
        {
            return NotFound(new { message = $"Airport {id} not found" });
        }

        airport.SoftDelete();
        await _airportRepository.UpdateAsync(airport);
        return NoContent();
    }

    private static AirportManagementResponse Map(Airport airport)
    {
        return new AirportManagementResponse
        {
            AirportId = airport.Id,
            Code = airport.Code,
            Name = airport.Name,
            City = airport.City,
            Province = airport.Province,
            IsActive = airport.IsActive,
            IsDeleted = airport.IsDeleted
        };
    }

    private static void ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }
    }
}
