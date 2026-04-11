using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightsController(IFlightService flightService, ISearchFlightService searchService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] FlightSearchRequest request)
    {
        var results = await searchService.SearchAsync(request);
        return Ok(results);
    }

    [HttpGet("search/round-trip")]
    public async Task<IActionResult> SearchRoundTrip([FromQuery] RoundTripSearchRequest request)
    {
        var results = await searchService.SearchRoundTripAsync(request);
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var flight = await flightService.GetByIdAsync(id);
        return Ok(flight);
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var flights = await flightService.GetAllAsync(page, pageSize);
        return Ok(flights);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFlightRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var flight = await flightService.CreateAsync(request, adminId);
        return CreatedAtAction(nameof(GetById), new { id = flight.Id }, flight);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFlightRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var flight = await flightService.UpdateAsync(id, request, adminId);
        return Ok(flight);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await flightService.CancelFlightAsync(id, reason, adminId);
        return NoContent();
    }
}

[ApiController]
[Route("api/routes")]
public class RoutesController(IRouteService routeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var routes = await routeService.GetAllAsync();
        return Ok(routes);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var route = await routeService.CreateAsync(request, adminId);
        return Ok(route);
    }
}

[ApiController]
[Route("api/aircraft")]
public class AircraftController(IAircraftService aircraftService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var aircraft = await aircraftService.GetAllAsync();
        return Ok(aircraft);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAircraftRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var aircraft = await aircraftService.CreateAsync(request, adminId);
        return Ok(aircraft);
    }
}
