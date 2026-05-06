namespace API.Controllers;

using API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/debug/routes")]
[AllowAnonymous]
public class RoutesDebugController : ControllerBase
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<RoutesDebugController> _logger;

    public RoutesDebugController(FlightBookingDbContext context, ILogger<RoutesDebugController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all routes with airport codes
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetAllRoutes()
    {
        try
        {
            var routes = await _context.Routes
                .Include(r => r.DepartureAirport)
                .Include(r => r.ArrivalAirport)
                .Select(r => new
                {
                    r.Id,
                    DepartureCode = r.DepartureAirport.Code,
                    DepartureName = r.DepartureAirport.Name,
                    ArrivalCode = r.ArrivalAirport.Code,
                    ArrivalName = r.ArrivalAirport.Name,
                    Distance = r.DistanceKm,
                    EstimatedDuration = r.EstimatedDurationMinutes,
                    r.IsActive
                })
                .OrderBy(r => r.Id)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                totalRoutes = routes.Count,
                routes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting routes");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all airports
    /// </summary>
    [HttpGet("airports")]
    public async Task<IActionResult> GetAllAirports()
    {
        try
        {
            var airports = await _context.Airports
                .Select(a => new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.City,
                    a.Province,
                    a.IsActive
                })
                .OrderBy(a => a.Id)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                totalAirports = airports.Count,
                airports
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting airports");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create missing routes between all airports
    /// </summary>
    [HttpPost("create-all-routes")]
    public async Task<IActionResult> CreateAllRoutes()
    {
        try
        {
            var airports = await _context.Airports.Where(a => a.IsActive).ToListAsync();
            var existingRoutes = await _context.Routes.ToListAsync();
            var createdRoutes = new List<object>();

            foreach (var departure in airports)
            {
                foreach (var arrival in airports)
                {
                    // Skip same airport
                    if (departure.Id == arrival.Id)
                        continue;

                    // Check if route already exists
                    var routeExists = existingRoutes.Any(r =>
                        r.DepartureAirportId == departure.Id &&
                        r.ArrivalAirportId == arrival.Id);

                    if (!routeExists)
                    {
                        // Calculate estimated distance and duration (simplified)
                        var distance = CalculateDistance(departure.Code, arrival.Code);
                        var duration = (int)(distance / 800.0 * 60); // Assume 800 km/h average speed

                        var route = new Domain.Entities.Route
                        {
                            DepartureAirportId = departure.Id,
                            ArrivalAirportId = arrival.Id,
                            DistanceKm = distance,
                            EstimatedDurationMinutes = duration,
                            IsActive = true
                        };

                        _context.Routes.Add(route);
                        await _context.SaveChangesAsync();

                        createdRoutes.Add(new
                        {
                            routeId = route.Id,
                            from = departure.Code,
                            to = arrival.Code,
                            distance,
                            duration
                        });
                    }
                }
            }

            return Ok(new
            {
                success = true,
                message = $"Created {createdRoutes.Count} new routes",
                totalAirports = airports.Count,
                possibleRoutes = airports.Count * (airports.Count - 1),
                createdRoutes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating routes");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private int CalculateDistance(string fromCode, string toCode)
    {
        // Simplified distance calculation based on known Vietnamese airports
        var distances = new Dictionary<(string, string), int>
        {
            // SGN routes
            { ("SGN", "HAN"), 1166 }, { ("HAN", "SGN"), 1166 },
            { ("SGN", "DAD"), 608 }, { ("DAD", "SGN"), 608 },
            { ("SGN", "CTS"), 3800 }, { ("CTS", "SGN"), 3800 },
            { ("SGN", "VCA"), 1200 }, { ("VCA", "SGN"), 1200 },
            { ("SGN", "HUI"), 700 }, { ("HUI", "SGN"), 700 },
            
            // HAN routes
            { ("HAN", "DAD"), 616 }, { ("DAD", "HAN"), 616 },
            { ("HAN", "CTS"), 3500 }, { ("CTS", "HAN"), 3500 },
            { ("HAN", "VCA"), 1400 }, { ("VCA", "HAN"), 1400 },
            { ("HAN", "HUI"), 540 }, { ("HUI", "HAN"), 540 },
            
            // DAD routes
            { ("DAD", "CTS"), 3600 }, { ("CTS", "DAD"), 3600 },
            { ("DAD", "VCA"), 800 }, { ("VCA", "DAD"), 800 },
            { ("DAD", "HUI"), 100 }, { ("HUI", "DAD"), 100 },
            
            // Other routes
            { ("CTS", "VCA"), 4000 }, { ("VCA", "CTS"), 4000 },
            { ("CTS", "HUI"), 3700 }, { ("HUI", "CTS"), 3700 },
            { ("VCA", "HUI"), 900 }, { ("HUI", "VCA"), 900 }
        };

        return distances.TryGetValue((fromCode, toCode), out var distance) ? distance : 1000;
    }
}
