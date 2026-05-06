namespace API.Controllers;

using API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/admin/database")]
[AllowAnonymous] // Remove this in production!
public class DatabaseFixController : ControllerBase
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<DatabaseFixController> _logger;

    public DatabaseFixController(FlightBookingDbContext context, ILogger<DatabaseFixController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Fix payment unique constraint issue
    /// </summary>
    [HttpPost("fix-payment-constraint")]
    public async Task<IActionResult> FixPaymentConstraint()
    {
        try
        {
            _logger.LogInformation("Starting payment constraint fix...");

            // Drop unique index and create non-unique index
            var sql = @"
                -- Drop the unique index if it exists
                DROP INDEX IF EXISTS ""IX_Payments_BookingId"";
                
                -- Create a non-unique index for performance
                CREATE INDEX IF NOT EXISTS ""IX_Payments_BookingId"" ON ""Payments"" (""BookingId"");
            ";

            await _context.Database.ExecuteSqlRawAsync(sql);

            _logger.LogInformation("Payment constraint fixed successfully");

            return Ok(new
            {
                success = true,
                message = "Payment constraint fixed. Multiple payments per booking are now allowed.",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing payment constraint");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Delete all payments for a specific booking
    /// </summary>
    [HttpDelete("delete-booking-payments/{bookingId}")]
    public async Task<IActionResult> DeleteBookingPayments(int bookingId)
    {
        try
        {
            var sql = @"DELETE FROM ""Payments"" WHERE ""BookingId"" = {0}";
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, bookingId);

            return Ok(new
            {
                success = true,
                message = $"Deleted {rowsAffected} payment(s) for booking {bookingId}",
                bookingId,
                rowsAffected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payments for booking {BookingId}", bookingId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Check current payment indexes
    /// </summary>
    [HttpGet("check-payment-indexes")]
    public async Task<IActionResult> CheckPaymentIndexes()
    {
        try
        {
            var sql = @"
                SELECT 
                    indexname,
                    indexdef
                FROM pg_indexes
                WHERE tablename = 'Payments'
                ORDER BY indexname
            ";

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var indexes = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add(new
                {
                    indexName = reader.GetString(0),
                    indexDefinition = reader.GetString(1)
                });
            }

            return Ok(new
            {
                success = true,
                tableName = "Payments",
                indexCount = indexes.Count,
                indexes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment indexes");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
