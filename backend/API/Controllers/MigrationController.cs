namespace API.Controllers;

using API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/admin/migration")]
[AllowAnonymous] // REMOVE THIS IN PRODUCTION!
public class MigrationController : ControllerBase
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<MigrationController> _logger;
    private readonly IWebHostEnvironment _env;

    public MigrationController(
        FlightBookingDbContext context,
        ILogger<MigrationController> logger,
        IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Run FlightDefinition migration
    /// </summary>
    [HttpPost("flight-definition")]
    public async Task<IActionResult> MigrateToFlightDefinition()
    {
        try
        {
            // Safety check - only allow in development
            if (!_env.IsDevelopment())
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Migration is only allowed in Development environment"
                });
            }

            _logger.LogWarning("⚠️ FLIGHT DEFINITION MIGRATION INITIATED");

            // Check if table already exists
            var tableExists = false;
            try
            {
                await _context.FlightDefinitions.AnyAsync();
                tableExists = true;
                _logger.LogInformation("FlightDefinitions table already exists, skipping table creation");
            }
            catch
            {
                _logger.LogInformation("FlightDefinitions table does not exist, will create it");
            }

            if (!tableExists)
            {
                var sql = await System.IO.File.ReadAllTextAsync("migrate_to_flight_definition.sql");

                _logger.LogInformation("Executing migration SQL...");
                await _context.Database.ExecuteSqlRawAsync(sql);

                _logger.LogInformation("✓ Migration completed");
            }

            // Now run sample data
            _logger.LogInformation("Loading sample flight definitions...");
            var sampleSql = await System.IO.File.ReadAllTextAsync("sample_flight_definitions.sql");
            
            try
            {
                await _context.Database.ExecuteSqlRawAsync(sampleSql);
                _logger.LogInformation("✓ Sample data loaded");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sample data may already exist, continuing...");
            }

            // Get counts
            var definitionCount = await _context.FlightDefinitions.CountAsync();
            var flightCount = await _context.Flights.CountAsync();

            return Ok(new
            {
                success = true,
                message = "FlightDefinition migration completed successfully!",
                tableAlreadyExisted = tableExists,
                flightDefinitions = definitionCount,
                flights = flightCount,
                steps = tableExists 
                    ? new[]
                    {
                        "✓ FlightDefinitions table already exists",
                        "✓ Sample flight definitions loaded/updated",
                        $"✓ Total FlightDefinitions: {definitionCount}",
                        $"✓ Total Flights: {flightCount}"
                    }
                    : new[]
                    {
                        "✓ FlightDefinitions table created",
                        "✓ Existing flights migrated to FlightDefinitions",
                        "✓ Flights table updated with FlightDefinitionId",
                        "✓ Foreign keys and indexes created",
                        "✓ Sample flight definitions loaded",
                        $"✓ Total FlightDefinitions: {definitionCount}",
                        $"✓ Total Flights: {flightCount}"
                    },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running migration");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// DANGER: Clean recreation - Drop and recreate Flights tables
    /// </summary>
    [HttpPost("recreate-flights-clean")]
    public async Task<IActionResult> RecreateFlightsClean()
    {
        try
        {
            // Safety check - only allow in development
            if (!_env.IsDevelopment())
            {
                return BadRequest(new
                {
                    success = false,
                    error = "This operation is only allowed in Development environment"
                });
            }

            _logger.LogWarning("⚠️ CLEAN RECREATION INITIATED - ALL FLIGHT DATA WILL BE LOST!");

            // Step 0: Create AircraftSeatTemplates if not exists
            var templateCount = await _context.AircraftSeatTemplates.CountAsync();
            if (templateCount == 0)
            {
                _logger.LogInformation("Creating AircraftSeatTemplates...");
                var templateSql = await System.IO.File.ReadAllTextAsync("sample_aircraft_seat_templates.sql");
                await _context.Database.ExecuteSqlRawAsync(templateSql);
                _logger.LogInformation("✓ AircraftSeatTemplates created");
            }

            // Step 1: Drop and recreate tables
            var recreateSql = await System.IO.File.ReadAllTextAsync("recreate_flights_clean.sql");
            await _context.Database.ExecuteSqlRawAsync(recreateSql);
            _logger.LogInformation("✓ Tables recreated");

            // Step 2: Load FlightDefinitions sample data
            var sampleSql = await System.IO.File.ReadAllTextAsync("sample_flight_definitions.sql");
            await _context.Database.ExecuteSqlRawAsync(sampleSql);
            _logger.LogInformation("✓ FlightDefinitions sample data loaded");

            // Step 3: Generate Flights from FlightDefinitions (next 30 days)
            var generateFlightsSql = @"
                -- Generate flights for next 30 days from FlightDefinitions
                INSERT INTO ""Flights"" (
                    ""FlightDefinitionId"",
                    ""DepartureTime"",
                    ""ArrivalTime"",
                    ""Status"",
                    ""CreatedAt"",
                    ""UpdatedAt""
                )
                SELECT 
                    fd.""Id"" as ""FlightDefinitionId"",
                    (date_series.date + fd.""DepartureTime"") as ""DepartureTime"",
                    (date_series.date + fd.""ArrivalTime"" + (fd.""ArrivalOffsetDays"" || ' days')::INTERVAL) as ""ArrivalTime"",
                    0 as ""Status"",
                    CURRENT_TIMESTAMP as ""CreatedAt"",
                    CURRENT_TIMESTAMP as ""UpdatedAt""
                FROM ""FlightDefinitions"" fd
                CROSS JOIN (
                    SELECT generate_series(
                        CURRENT_DATE,
                        CURRENT_DATE + INTERVAL '30 days',
                        '1 day'::INTERVAL
                    )::DATE as date
                ) date_series
                WHERE fd.""IsActive"" = TRUE
                  AND (fd.""OperatingDays"" & (1 << EXTRACT(DOW FROM date_series.date)::INTEGER)) > 0;
            ";
            
            await _context.Database.ExecuteSqlRawAsync(generateFlightsSql);
            _logger.LogInformation("✓ Flights generated for next 30 days");

            // Step 4: Create FlightSeatInventories for all flights
            var createInventorySql = @"
                -- Create seat inventories for all flights
                INSERT INTO ""FlightSeatInventories"" (
                    ""FlightId"",
                    ""SeatClassId"",
                    ""TotalSeats"",
                    ""AvailableSeats"",
                    ""HeldSeats"",
                    ""SoldSeats"",
                    ""BasePrice"",
                    ""CurrentPrice"",
                    ""CreatedAt"",
                    ""UpdatedAt""
                )
                SELECT 
                    f.""Id"" as ""FlightId"",
                    ast.""SeatClassId"",
                    ast.""DefaultSeatCount"" as ""TotalSeats"",
                    ast.""DefaultSeatCount"" as ""AvailableSeats"",
                    0 as ""HeldSeats"",
                    0 as ""SoldSeats"",
                    ast.""DefaultBasePrice"" as ""BasePrice"",
                    ast.""DefaultBasePrice"" as ""CurrentPrice"",
                    CURRENT_TIMESTAMP as ""CreatedAt"",
                    CURRENT_TIMESTAMP as ""UpdatedAt""
                FROM ""Flights"" f
                JOIN ""FlightDefinitions"" fd ON f.""FlightDefinitionId"" = fd.""Id""
                JOIN ""AircraftSeatTemplates"" ast ON ast.""AircraftId"" = fd.""DefaultAircraftId""
                WHERE ast.""IsDeleted"" = FALSE;
            ";
            
            await _context.Database.ExecuteSqlRawAsync(createInventorySql);
            _logger.LogInformation("✓ FlightSeatInventories created");

            // Get counts
            var definitionCount = await _context.FlightDefinitions.CountAsync();
            var flightCount = await _context.Flights.CountAsync();
            var inventoryCount = await _context.FlightSeatInventories.CountAsync();

            return Ok(new
            {
                success = true,
                message = "Flights tables recreated successfully with clean structure!",
                counts = new
                {
                    flightDefinitions = definitionCount,
                    flights = flightCount,
                    flightSeatInventories = inventoryCount
                },
                steps = new[]
                {
                    "✓ Deleted all dependent data (Bookings, Payments, Tickets)",
                    "✓ Dropped old Flights table",
                    "✓ Dropped old FlightDefinitions table",
                    "✓ Created new FlightDefinitions table",
                    "✓ Created new Flights table (with FlightDefinitionId)",
                    "✓ Loaded FlightDefinitions sample data (20 definitions)",
                    $"✓ Generated {flightCount} flights for next 30 days",
                    $"✓ Created {inventoryCount} seat inventories"
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recreating flights tables");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Complete Flight migration by dropping old columns
    /// </summary>
    [HttpPost("complete-flight-migration")]
    public async Task<IActionResult> CompleteFlightMigration()
    {
        try
        {
            // Safety check - only allow in development
            if (!_env.IsDevelopment())
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Migration is only allowed in Development environment"
                });
            }

            _logger.LogWarning("⚠️ COMPLETING FLIGHT MIGRATION - DROPPING OLD COLUMNS");

            // Read and execute the completion script
            var sql = await System.IO.File.ReadAllTextAsync("complete_flight_migration.sql");
            
            await _context.Database.ExecuteSqlRawAsync(sql);

            _logger.LogInformation("✓ Migration completed - old columns dropped");

            // Verify final structure
            var columns = await _context.Database.SqlQuery<string>(
                $@"SELECT column_name 
                   FROM information_schema.columns 
                   WHERE table_name = 'Flights' 
                   ORDER BY ordinal_position"
            ).ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Flight migration completed! Old columns dropped.",
                finalColumns = columns,
                steps = new[]
                {
                    "✓ Verified all flights have FlightDefinitionId",
                    "✓ Dropped old indexes (IX_Flights_FlightNumber, etc.)",
                    "✓ Dropped old foreign keys (FK_Flights_Routes, FK_Flights_Aircraft)",
                    "✓ Dropped old columns (FlightNumber, RouteId, AircraftId)",
                    "✓ Migration fully completed"
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing migration");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Check Flights table structure
    /// </summary>
    [HttpGet("check-flights-structure")]
    public async Task<IActionResult> CheckFlightsStructure()
    {
        try
        {
            // Get all column names from Flights table
            var columns = await _context.Database.SqlQuery<string>(
                $@"SELECT column_name 
                   FROM information_schema.columns 
                   WHERE table_name = 'Flights' 
                   ORDER BY ordinal_position"
            ).ToListAsync();

            var hasFlightDefinitionId = columns.Contains("FlightDefinitionId");
            var hasActualAircraftId = columns.Contains("ActualAircraftId");
            var hasOldFlightNumber = columns.Contains("FlightNumber");
            var hasOldRouteId = columns.Contains("RouteId");
            var hasOldAircraftId = columns.Contains("AircraftId");

            return Ok(new
            {
                success = true,
                columns,
                analysis = new
                {
                    hasFlightDefinitionId,
                    hasActualAircraftId,
                    hasOldFlightNumber,
                    hasOldRouteId,
                    hasOldAircraftId,
                    status = hasFlightDefinitionId && hasActualAircraftId && !hasOldFlightNumber && !hasOldRouteId && !hasOldAircraftId
                        ? "FULLY_MIGRATED"
                        : hasFlightDefinitionId && hasActualAircraftId
                            ? "PARTIALLY_MIGRATED"
                            : "NOT_MIGRATED"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Flights structure");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create AircraftSeatTemplates sample data
    /// </summary>
    [HttpPost("create-aircraft-seat-templates")]
    public async Task<IActionResult> CreateAircraftSeatTemplates()
    {
        try
        {
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { success = false, error = "Only allowed in Development" });
            }

            _logger.LogInformation("Creating AircraftSeatTemplates...");

            // Check if already exists
            var existingCount = await _context.AircraftSeatTemplates.CountAsync();
            if (existingCount > 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "AircraftSeatTemplates already exist",
                    count = existingCount
                });
            }

            var sql = await System.IO.File.ReadAllTextAsync("sample_aircraft_seat_templates.sql");
            await _context.Database.ExecuteSqlRawAsync(sql);

            var newCount = await _context.AircraftSeatTemplates.CountAsync();

            return Ok(new
            {
                success = true,
                message = "AircraftSeatTemplates created successfully",
                count = newCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AircraftSeatTemplates");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Generate FlightSeatInventories from existing flights and templates
    /// </summary>
    [HttpPost("generate-flight-seat-inventories")]
    public async Task<IActionResult> GenerateFlightSeatInventories()
    {
        try
        {
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { success = false, error = "Only allowed in Development" });
            }

            _logger.LogInformation("Generating FlightSeatInventories...");

            // Delete existing
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"FlightSeatInventories\"");

            // Generate new
            var sql = @"
                INSERT INTO ""FlightSeatInventories"" (
                    ""FlightId"",
                    ""SeatClassId"",
                    ""TotalSeats"",
                    ""AvailableSeats"",
                    ""HeldSeats"",
                    ""SoldSeats"",
                    ""BasePrice"",
                    ""CurrentPrice"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""IsDeleted"",
                    ""DeletedAt""
                )
                SELECT 
                    f.""Id"" as ""FlightId"",
                    ast.""SeatClassId"",
                    ast.""DefaultSeatCount"" as ""TotalSeats"",
                    ast.""DefaultSeatCount"" as ""AvailableSeats"",
                    0 as ""HeldSeats"",
                    0 as ""SoldSeats"",
                    ast.""DefaultBasePrice"" as ""BasePrice"",
                    ast.""DefaultBasePrice"" as ""CurrentPrice"",
                    CURRENT_TIMESTAMP as ""CreatedAt"",
                    CURRENT_TIMESTAMP as ""UpdatedAt"",
                    false as ""IsDeleted"",
                    NULL as ""DeletedAt""
                FROM ""Flights"" f
                INNER JOIN ""FlightDefinitions"" fd ON f.""FlightDefinitionId"" = fd.""Id""
                INNER JOIN ""AircraftSeatTemplates"" ast ON 
                    COALESCE(f.""ActualAircraftId"", fd.""DefaultAircraftId"") = ast.""AircraftId""
                WHERE ast.""IsDeleted"" = false;
            ";

            await _context.Database.ExecuteSqlRawAsync(sql);

            var count = await _context.FlightSeatInventories.CountAsync();

            return Ok(new
            {
                success = true,
                message = "FlightSeatInventories generated successfully",
                count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FlightSeatInventories");
            return StatusCode(500, new { success = false, error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// FIX: Complete fix for flight search - creates templates and inventories
    /// </summary>
    [HttpPost("fix-search")]
    public async Task<IActionResult> FixSearch()
    {
        try
        {
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { success = false, error = "Only allowed in Development" });
            }

            _logger.LogInformation("Running complete search fix...");

            var sql = await System.IO.File.ReadAllTextAsync("fix_search_complete.sql");
            await _context.Database.ExecuteSqlRawAsync(sql);

            var templateCount = await _context.AircraftSeatTemplates.CountAsync();
            var inventoryCount = await _context.FlightSeatInventories.CountAsync();
            var flightCount = await _context.Flights.CountAsync();

            return Ok(new
            {
                success = true,
                message = "Flight search fixed! AircraftSeatTemplates and FlightSeatInventories created.",
                counts = new
                {
                    aircraftSeatTemplates = templateCount,
                    flightSeatInventories = inventoryCount,
                    flights = flightCount
                },
                steps = new[]
                {
                    "✓ Created AircraftSeatTemplates for all aircraft",
                    "✓ Generated FlightSeatInventories for all flights",
                    "✓ Flight search should now return results"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing search");
            return StatusCode(500, new { success = false, error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Check migration status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetMigrationStatus()
    {
        try
        {
            var definitionCount = 0;
            var flightCount = 0;
            
            try
            {
                definitionCount = await _context.FlightDefinitions.CountAsync();
                flightCount = await _context.Flights.CountAsync();
            }
            catch { }

            return Ok(new
            {
                flightDefinitions = definitionCount,
                flights = flightCount,
                message = "Use /check-flights-structure for detailed column analysis"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking migration status");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
