namespace API.Controllers;

using API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/admin/database")]
[AllowAnonymous] // REMOVE THIS IN PRODUCTION!
public class DatabaseResetController : ControllerBase
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<DatabaseResetController> _logger;
    private readonly IWebHostEnvironment _env;

    public DatabaseResetController(
        FlightBookingDbContext context, 
        ILogger<DatabaseResetController> logger,
        IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// DANGER: Reset entire database - Drop and recreate all tables
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetDatabase()
    {
        try
        {
            // Safety check - only allow in development
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { 
                    success = false, 
                    error = "Database reset is only allowed in Development environment" 
                });
            }

            _logger.LogWarning("⚠️ DATABASE RESET INITIATED - ALL DATA WILL BE LOST!");

            // Delete the database
            await _context.Database.EnsureDeletedAsync();
            _logger.LogInformation("✓ Database deleted");

            // Recreate the database
            await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("✓ Database recreated");

            // Run migrations
            await _context.Database.MigrateAsync();
            _logger.LogInformation("✓ Migrations applied");

            // Seed data
            // TODO: DbInitializer and SampleDataForSearching need refactoring for FlightDefinition
            // await DbInitializer.InitializeDatabaseAsync(_context);
            _logger.LogInformation("⚠ Sample data seeding skipped (needs FlightDefinition refactoring)");

            // Add search test data
            // await SampleDataForSearching.AddSearchTestDataAsync(_context);
            _logger.LogInformation("⚠ Search test data skipped (needs FlightDefinition refactoring)");

            return Ok(new
            {
                success = true,
                message = "Database reset successfully!",
                steps = new[]
                {
                    "✓ Database deleted",
                    "✓ Database recreated",
                    "✓ Migrations applied",
                    "⚠ Sample data seeding skipped (needs FlightDefinition refactoring)",
                    "⚠ Search test data skipped (needs FlightDefinition refactoring)"
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting database");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Clear all data but keep schema
    /// </summary>
    [HttpPost("clear-data")]
    public async Task<IActionResult> ClearAllData()
    {
        try
        {
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { 
                    success = false, 
                    error = "Data clearing is only allowed in Development environment" 
                });
            }

            _logger.LogWarning("⚠️ CLEARING ALL DATA FROM DATABASE");

            var deletedCounts = new Dictionary<string, int>();

            // Delete in correct order (respecting foreign keys)
            deletedCounts["Tickets"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Tickets\"");
            deletedCounts["RefundRequests"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"RefundRequests\"");
            deletedCounts["Payments"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Payments\"");
            deletedCounts["BookingPassengers"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"BookingPassengers\"");
            deletedCounts["BookingServices"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"BookingServices\"");
            deletedCounts["Bookings"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Bookings\"");
            deletedCounts["FlightSeatInventories"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"FlightSeatInventories\"");
            deletedCounts["Flights"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Flights\"");
            deletedCounts["PromotionUsages"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"PromotionUsages\"");
            deletedCounts["Promotions"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Promotions\"");
            deletedCounts["Routes"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Routes\"");
            deletedCounts["AircraftSeatTemplates"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"AircraftSeatTemplates\"");
            deletedCounts["SeatClasses"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"SeatClasses\"");
            deletedCounts["Aircraft"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Aircraft\"");
            deletedCounts["Airports"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Airports\"");
            deletedCounts["NotificationLogs"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"NotificationLogs\"");
            deletedCounts["AuditLogs"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"AuditLogs\"");
            deletedCounts["PasswordResetTokens"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"PasswordResetTokens\"");
            deletedCounts["EmailVerificationTokens"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"EmailVerificationTokens\"");
            deletedCounts["UserRoles"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"UserRoles\"");
            deletedCounts["Users"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Users\"");
            deletedCounts["Roles"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Roles\"");
            deletedCounts["RefundPolicies"] = await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"RefundPolicies\"");

            // Reset sequences
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT setval(pg_get_serial_sequence('""Users""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""Roles""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""Airports""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""Aircraft""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""Routes""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""SeatClasses""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""Flights""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""Bookings""', 'Id'), 1, false);
                SELECT setval(pg_get_serial_sequence('""Payments""', 'Id'), 1, false);
            ");

            _logger.LogInformation("✓ All data cleared and sequences reset");

            return Ok(new
            {
                success = true,
                message = "All data cleared successfully. Database schema preserved.",
                deletedCounts,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing data");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Reseed sample data
    /// </summary>
    [HttpPost("reseed")]
    public async Task<IActionResult> ReseedData()
    {
        try
        {
            _logger.LogInformation("Reseeding database...");

            // TODO: DbInitializer and SampleDataForSearching need refactoring for FlightDefinition
            // await DbInitializer.InitializeDatabaseAsync(_context);
            // await SampleDataForSearching.AddSearchTestDataAsync(_context);

            return Ok(new
            {
                success = false,
                message = "Database reseeding temporarily disabled (needs FlightDefinition refactoring)",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reseeding data");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get database statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDatabaseStats()
    {
        try
        {
            var stats = new Dictionary<string, int>
            {
                ["Users"] = await _context.Users.CountAsync(),
                ["Roles"] = await _context.Roles.CountAsync(),
                ["Airports"] = await _context.Airports.CountAsync(),
                ["Aircraft"] = await _context.Aircraft.CountAsync(),
                ["Routes"] = await _context.Routes.CountAsync(),
                ["SeatClasses"] = await _context.SeatClasses.CountAsync(),
                ["Flights"] = await _context.Flights.CountAsync(),
                ["FlightSeatInventories"] = await _context.FlightSeatInventories.CountAsync(),
                ["Bookings"] = await _context.Bookings.CountAsync(),
                ["BookingPassengers"] = await _context.BookingPassengers.CountAsync(),
                ["Payments"] = await _context.Payments.CountAsync(),
                ["Tickets"] = await _context.Tickets.CountAsync(),
                ["Promotions"] = await _context.Promotions.CountAsync(),
                ["RefundRequests"] = await _context.RefundRequests.CountAsync(),
                ["AuditLogs"] = await _context.AuditLogs.CountAsync(),
                ["NotificationLogs"] = await _context.NotificationLogs.CountAsync()
            };

            return Ok(new
            {
                success = true,
                databaseName = "FlightBookingDB",
                stats,
                totalRecords = stats.Values.Sum(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database stats");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
