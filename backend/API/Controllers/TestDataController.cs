namespace API.Controllers;

using API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/test-data")]
[AllowAnonymous] // Chỉ dùng cho môi trường development
public class TestDataController : ControllerBase
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<TestDataController> _logger;

    public TestDataController(FlightBookingDbContext context, ILogger<TestDataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Cập nhật thời gian khởi hành của các chuyến bay đã qua về tương lai
    /// </summary>
    [HttpPost("refresh-flight-dates")]
    public async Task<IActionResult> RefreshFlightDates()
    {
        try
        {
            var now = DateTime.UtcNow;
            var pastFlights = await _context.Flights
                .Where(f => f.DepartureTime < now)
                .ToListAsync();

            if (pastFlights.Count == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "No flights need updating",
                    updatedCount = 0
                });
            }

            foreach (var flight in pastFlights)
            {
                // Giữ nguyên giờ trong ngày, chỉ đổi ngày sang ngày mai
                var timeOfDay = flight.DepartureTime.TimeOfDay;
                var duration = flight.ArrivalTime - flight.DepartureTime;
                
                flight.DepartureTime = now.Date.AddDays(1).Add(timeOfDay);
                flight.ArrivalTime = flight.DepartureTime.Add(duration);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated {Count} flights to future dates", pastFlights.Count);

            return Ok(new
            {
                success = true,
                message = $"Updated {pastFlights.Count} flights",
                updatedCount = pastFlights.Count,
                flights = pastFlights.Select(f => new
                {
                    f.Id,
                    f.FlightNumber,
                    departureTime = f.DepartureTime,
                    arrivalTime = f.ArrivalTime
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing flight dates");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Kiểm tra các chuyến bay đã qua
    /// </summary>
    [HttpGet("check-past-flights")]
    public async Task<IActionResult> CheckPastFlights()
    {
        try
        {
            var now = DateTime.UtcNow;
            var pastFlights = await _context.Flights
                .Where(f => f.DepartureTime < now)
                .Select(f => new
                {
                    f.Id,
                    f.FlightNumber,
                    departureTime = f.DepartureTime,
                    arrivalTime = f.ArrivalTime,
                    hoursAgo = (now - f.DepartureTime).TotalHours
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                currentTime = now,
                pastFlightsCount = pastFlights.Count,
                pastFlights
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking past flights");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
