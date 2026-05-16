using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Infrastructure.Data;

/// <summary>
/// Smart database schema synchronization that detects and applies only the differences
/// between EF Core entity models and the actual database schema.
/// </summary>
public static class DatabaseSchemaSync
{
    public static async Task SyncSchemaAsync(FlightBookingDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Starting smart schema synchronization...");

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            // Get all tables from database
            var existingTables = await GetExistingTablesAsync(connection);
            logger.LogInformation("Found {Count} existing tables in database", existingTables.Count);

            // Get all columns for each table
            var existingColumns = await GetExistingColumnsAsync(connection);
            
            // Define expected schema from entities
            var expectedSchema = GetExpectedSchema();

            // Generate ALTER TABLE statements for missing columns
            var alterStatements = new List<string>();

            foreach (var (tableName, expectedColumns) in expectedSchema)
            {
                if (!existingTables.Contains(tableName))
                {
                    // Table doesn't exist - create it
                    logger.LogWarning("Table {TableName} does not exist - will create it", tableName);
                    var createTableSql = GenerateCreateTableStatement(tableName, expectedColumns);
                    alterStatements.Add(createTableSql);
                    continue;
                }

                var tableColumns = existingColumns
                    .Where(c => c.TableName == tableName)
                    .Select(c => c.ColumnName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var (columnName, columnDef) in expectedColumns)
                {
                    if (!tableColumns.Contains(columnName))
                    {
                        var alterSql = $"ALTER TABLE \"{tableName}\" ADD COLUMN IF NOT EXISTS \"{columnName}\" {columnDef};";
                        alterStatements.Add(alterSql);
                        logger.LogInformation("Missing column detected: {TableName}.{ColumnName}", tableName, columnName);
                    }
                }
            }

            // Execute all ALTER statements
            if (alterStatements.Any())
            {
                logger.LogInformation("Applying {Count} schema changes...", alterStatements.Count);
                
                foreach (var sql in alterStatements)
                {
                    logger.LogDebug("Executing: {Sql}", sql);
                    await dbContext.Database.ExecuteSqlRawAsync(sql);
                }

                logger.LogInformation("✓ Schema synchronized successfully - {Count} columns added", alterStatements.Count);
            }
            else
            {
                logger.LogInformation("✓ Schema is already up to date - no changes needed");
            }

            await ApplyPostSyncDataFixesAsync(dbContext, logger);

            await connection.CloseAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to synchronize database schema");
            throw;
        }
    }

    private static async Task ApplyPostSyncDataFixesAsync(FlightBookingDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Applying post-sync data fixes...");

        await dbContext.Database.ExecuteSqlRawAsync(@"
            UPDATE ""Routes"" r
            SET ""Code"" = da.""Code"" || '-' || aa.""Code""
            FROM ""Airports"" da, ""Airports"" aa
            WHERE r.""DepartureAirportId"" = da.""Id""
              AND r.""ArrivalAirportId"" = aa.""Id""
              AND (r.""Code"" IS NULL OR btrim(r.""Code"") = '');
        ");

        await dbContext.Database.ExecuteSqlRawAsync(@"
            UPDATE ""Flights"" f
            SET
                ""FlightNumber"" = COALESCE(f.""FlightNumber"", fd.""FlightNumber""),
                ""RouteId"" = COALESCE(f.""RouteId"", fd.""RouteId""),
                ""AircraftId"" = COALESCE(f.""AircraftId"", f.""ActualAircraftId"", fd.""DefaultAircraftId""),
                ""ArrivalOffsetDays"" = COALESCE(f.""ArrivalOffsetDays"", fd.""ArrivalOffsetDays"", 0)
            FROM ""FlightDefinitions"" fd
            WHERE f.""FlightDefinitionId"" = fd.""Id""
              AND (f.""FlightNumber"" IS NULL OR f.""RouteId"" IS NULL OR f.""AircraftId"" IS NULL);
        ");

        await dbContext.Database.ExecuteSqlRawAsync(@"
            UPDATE ""FlightScheduleTemplates""
            SET ""Code"" = 'TPL_' || ""Id""
            WHERE ""Code"" IS NULL OR btrim(""Code"") = '';
        ");

        await dbContext.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""FlightTemplateDetails""
                DROP COLUMN IF EXISTS ""RouteId"",
                DROP COLUMN IF EXISTS ""AircraftId"",
                DROP COLUMN IF EXISTS ""DepartureTime"",
                DROP COLUMN IF EXISTS ""ArrivalTime"",
                DROP COLUMN IF EXISTS ""FlightNumberPrefix"",
                DROP COLUMN IF EXISTS ""FlightNumberSuffix"";
        ");

        await dbContext.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""FlightDefinitions""
                DROP COLUMN IF EXISTS ""OperatingDays"";
        ");
    }

    private static async Task<HashSet<string>> GetExistingTablesAsync(System.Data.Common.DbConnection connection)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_type = 'BASE TABLE'";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static string GenerateCreateTableStatement(string tableName, Dictionary<string, string> columns)
    {
        var columnDefinitions = new List<string>();
        
        foreach (var kvp in columns)
        {
            var columnName = kvp.Key;
            var columnDef = kvp.Value;
            
            // Convert "integer NOT NULL" to "SERIAL PRIMARY KEY" for Id column
            if (columnName == "Id" && columnDef.Contains("integer"))
            {
                columnDefinitions.Add($"\"{columnName}\" SERIAL PRIMARY KEY");
            }
            else
            {
                columnDefinitions.Add($"\"{columnName}\" {columnDef}");
            }
        }
        
        var columnsStr = string.Join(",\n    ", columnDefinitions);
        
        return $@"
CREATE TABLE IF NOT EXISTS ""{tableName}"" (
    {columnsStr}
);";
    }

    private static async Task<List<(string TableName, string ColumnName)>> GetExistingColumnsAsync(
        System.Data.Common.DbConnection connection)
    {
        var columns = new List<(string, string)>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT table_name, column_name 
            FROM information_schema.columns 
            WHERE table_schema = 'public'
            ORDER BY table_name, ordinal_position";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add((reader.GetString(0), reader.GetString(1)));
        }

        return columns;
    }

    /// <summary>
    /// Defines the expected schema based on entity models.
    /// Format: TableName -> (ColumnName -> ColumnDefinition)
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> GetExpectedSchema()
    {
        return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bookings"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["BookingCode"] = "varchar(50) NOT NULL",
                ["UserId"] = "integer NOT NULL",
                ["TripType"] = "integer NOT NULL DEFAULT 0",
                ["OutboundFlightId"] = "integer NOT NULL",
                ["ReturnFlightId"] = "integer NULL",
                ["Status"] = "integer NOT NULL DEFAULT 0",
                ["ContactEmail"] = "varchar(255) NOT NULL",
                ["ContactPhone"] = "varchar(20) NULL",
                ["TotalAmount"] = "numeric(10,2) NOT NULL",
                ["DiscountAmount"] = "numeric(10,2) NOT NULL DEFAULT 0",
                ["FinalAmount"] = "numeric(10,2) NOT NULL",
                ["Currency"] = "varchar(10) NOT NULL DEFAULT 'VND'",
                ["ExpiresAt"] = "timestamp with time zone NULL",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["UpdatedAt"] = "timestamp with time zone NOT NULL",
                ["PromotionId"] = "integer NULL",
                ["CreatedBy"] = "integer NULL",
                ["UpdatedBy"] = "integer NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL",
                ["Version"] = "integer NOT NULL DEFAULT 0"
            },
            ["Flights"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["FlightDefinitionId"] = "integer NULL", // Allow NULL initially, will be populated later
                ["FlightNumber"] = "varchar(20) NULL",
                ["RouteId"] = "integer NULL",
                ["AircraftId"] = "integer NULL",
                ["ArrivalOffsetDays"] = "integer NOT NULL DEFAULT 0",
                ["DepartureTime"] = "timestamp with time zone NOT NULL",
                ["ArrivalTime"] = "timestamp with time zone NOT NULL",
                ["ActualAircraftId"] = "integer NULL",
                ["Status"] = "integer NOT NULL DEFAULT 0",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["UpdatedAt"] = "timestamp with time zone NOT NULL",
                ["CreatedBy"] = "integer NULL",
                ["UpdatedBy"] = "integer NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL",
                ["Version"] = "integer NOT NULL DEFAULT 0"
            },
            ["Users"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Email"] = "varchar(255) NOT NULL",
                ["PasswordHash"] = "text NOT NULL",
                ["FullName"] = "varchar(255) NOT NULL",
                ["Phone"] = "varchar(20) NULL",
                ["GoogleId"] = "varchar(255) NULL",
                ["Status"] = "integer NOT NULL DEFAULT 0",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["UpdatedAt"] = "timestamp with time zone NOT NULL",
                ["IsEmailVerified"] = "boolean NOT NULL DEFAULT FALSE",
                ["FailedLoginAttempts"] = "integer NOT NULL DEFAULT 0",
                ["PasswordChangedAt"] = "timestamp with time zone NULL",
                ["LastLoginAt"] = "timestamp with time zone NULL",
                ["IsTwoFactorEnabled"] = "boolean NOT NULL DEFAULT FALSE",
                ["TwoFactorSecret"] = "varchar(500) NULL",
                ["PhoneNumberVerified"] = "boolean NOT NULL DEFAULT FALSE",
                ["DateOfBirth"] = "timestamp with time zone NULL",
                ["Nationality"] = "varchar(100) NULL",
                ["PassportExpiryDate"] = "timestamp with time zone NULL",
                ["PassportCountry"] = "varchar(100) NULL",
                ["Gender"] = "varchar(10) NULL",
                ["MaritalStatus"] = "varchar(50) NULL",
                ["Occupation"] = "varchar(255) NULL",
                ["Address"] = "varchar(500) NULL",
                ["City"] = "varchar(100) NULL",
                ["Country"] = "varchar(100) NULL",
                ["ZipCode"] = "varchar(20) NULL",
                ["PreferredLanguage"] = "varchar(10) NULL",
                ["PreferredCurrency"] = "varchar(10) NULL",
                ["TimeZone"] = "varchar(100) NULL",
                ["MarketingOptIn"] = "boolean NOT NULL DEFAULT FALSE",
                ["NewsletterSubscription"] = "boolean NOT NULL DEFAULT FALSE",
                ["NotificationPreferences"] = "jsonb NULL",
                ["CreatedBy"] = "integer NULL",
                ["UpdatedBy"] = "integer NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL",
                ["Version"] = "integer NOT NULL DEFAULT 0"
            },
            ["Payments"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["BookingId"] = "integer NOT NULL",
                ["Amount"] = "numeric(10,2) NOT NULL",
                ["Currency"] = "varchar(10) NOT NULL DEFAULT 'VND'",
                ["PaymentMethod"] = "integer NOT NULL DEFAULT 0",
                ["Status"] = "integer NOT NULL DEFAULT 0",
                ["TransactionId"] = "varchar(255) NULL",
                ["PaymentGatewayResponse"] = "text NULL",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["UpdatedAt"] = "timestamp with time zone NOT NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["Tickets"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["TicketNumber"] = "varchar(50) NOT NULL",
                ["BookingId"] = "integer NOT NULL",
                ["PassengerId"] = "integer NOT NULL",
                ["FlightId"] = "integer NOT NULL",
                ["SeatNumber"] = "varchar(10) NULL",
                ["SeatClassId"] = "integer NOT NULL",
                ["Price"] = "numeric(10,2) NOT NULL",
                ["Status"] = "integer NOT NULL DEFAULT 0",
                ["CheckInTime"] = "timestamp with time zone NULL",
                ["BoardingTime"] = "timestamp with time zone NULL",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["UpdatedAt"] = "timestamp with time zone NOT NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["RefundRequests"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["BookingId"] = "integer NOT NULL",
                ["RequestedAmount"] = "numeric(10,2) NOT NULL",
                ["ApprovedAmount"] = "numeric(10,2) NULL",
                ["Reason"] = "text NULL",
                ["Status"] = "integer NOT NULL DEFAULT 0",
                ["RequestedAt"] = "timestamp with time zone NOT NULL",
                ["ProcessedAt"] = "timestamp with time zone NULL",
                ["ProcessedBy"] = "integer NULL",
                ["RefundMethod"] = "integer NULL",
                ["RefundTransactionId"] = "varchar(255) NULL",
                ["Notes"] = "text NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["Aircraft"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Model"] = "varchar(100) NOT NULL",
                ["RegistrationNumber"] = "varchar(50) NOT NULL",
                ["TotalSeats"] = "integer NOT NULL",
                ["IsActive"] = "boolean NOT NULL DEFAULT TRUE",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["Airports"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Code"] = "varchar(10) NOT NULL",
                ["Name"] = "varchar(255) NOT NULL",
                ["City"] = "varchar(100) NOT NULL",
                ["Province"] = "varchar(100) NULL",
                ["IsActive"] = "boolean NOT NULL DEFAULT TRUE",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["Routes"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Code"] = "varchar(50) NULL",
                ["DepartureAirportId"] = "integer NOT NULL",
                ["ArrivalAirportId"] = "integer NOT NULL",
                ["DistanceKm"] = "integer NOT NULL",
                ["EstimatedDurationMinutes"] = "integer NOT NULL",
                ["IsActive"] = "boolean NOT NULL DEFAULT TRUE",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["Roles"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Name"] = "varchar(50) NOT NULL",
                ["Description"] = "varchar(500) NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["SeatClasses"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Code"] = "varchar(20) NOT NULL",
                ["Name"] = "varchar(100) NOT NULL",
                ["RefundPercent"] = "numeric(5,2) NOT NULL",
                ["ChangeFee"] = "numeric(10,2) NOT NULL",
                ["Priority"] = "integer NOT NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["FlightSeatInventories"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["FlightId"] = "integer NOT NULL",
                ["SeatClassId"] = "integer NOT NULL",
                ["TotalSeats"] = "integer NOT NULL",
                ["AvailableSeats"] = "integer NOT NULL",
                ["BasePrice"] = "numeric(10,2) NOT NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["AircraftSeatTemplates"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["AircraftId"] = "integer NOT NULL",
                ["SeatClassId"] = "integer NOT NULL",
                ["DefaultSeatCount"] = "integer NOT NULL",
                ["DefaultBasePrice"] = "numeric(10,2) NOT NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL"
            },
            ["Promotions"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Code"] = "varchar(50) NOT NULL",
                ["DiscountType"] = "integer NOT NULL DEFAULT 0",
                ["DiscountValue"] = "numeric(10,2) NOT NULL",
                ["ValidFrom"] = "timestamp with time zone NOT NULL",
                ["ValidTo"] = "timestamp with time zone NOT NULL",
                ["UsageLimit"] = "integer NULL",
                ["UsedCount"] = "integer NOT NULL DEFAULT 0",
                ["IsActive"] = "boolean NOT NULL DEFAULT TRUE",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["CreatedBy"] = "integer NULL",
                ["UpdatedBy"] = "integer NULL",
                ["IsDeleted"] = "boolean NOT NULL DEFAULT FALSE",
                ["DeletedAt"] = "timestamp with time zone NULL",
                ["Version"] = "integer NOT NULL DEFAULT 0"
            },
            ["PromotionUsages"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["PromotionId"] = "integer NOT NULL",
                ["BookingId"] = "integer NOT NULL",
                ["UserId"] = "integer NOT NULL",
                ["DiscountAmount"] = "numeric(10,2) NOT NULL",
                ["UsedAt"] = "timestamp with time zone NOT NULL"
            },
            ["BookingPassengers"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["BookingId"] = "integer NOT NULL",
                ["PassengerType"] = "integer NOT NULL",
                ["Title"] = "varchar(10) NULL",
                ["FirstName"] = "varchar(100) NOT NULL",
                ["LastName"] = "varchar(100) NOT NULL",
                ["DateOfBirth"] = "timestamp with time zone NULL",
                ["Gender"] = "varchar(10) NULL",
                ["Nationality"] = "varchar(100) NULL",
                ["PassportNumber"] = "varchar(50) NULL",
                ["PassportExpiry"] = "timestamp with time zone NULL",
                ["PassportCountry"] = "varchar(100) NULL"
            },
            ["FlightScheduleTemplates"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["Code"] = "varchar(50) NULL",
                ["Name"] = "varchar(200) NOT NULL",
                ["Description"] = "varchar(1000) NULL",
                ["EffectiveFrom"] = "date NULL",
                ["EffectiveTo"] = "date NULL",
                ["IsActive"] = "boolean NOT NULL DEFAULT TRUE",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["UpdatedAt"] = "timestamp with time zone NOT NULL"
            },
            ["FlightTemplateDetails"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["TemplateId"] = "integer NOT NULL",
                ["FlightDefinitionId"] = "integer NULL",
                ["DayOfWeek"] = "integer NOT NULL",
                ["AircraftOverrideId"] = "integer NULL",
                ["DepartureTimeOverride"] = "time without time zone NULL",
                ["ArrivalTimeOverride"] = "time without time zone NULL",
                ["ArrivalOffsetDaysOverride"] = "integer NULL",
                ["IsActive"] = "boolean NOT NULL DEFAULT TRUE",
                ["CreatedAt"] = "timestamp with time zone NOT NULL"
            },
            ["FlightDefinitions"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "integer NOT NULL",
                ["FlightNumber"] = "varchar(20) NOT NULL",
                ["RouteId"] = "integer NOT NULL",
                ["DefaultAircraftId"] = "integer NOT NULL",
                ["DepartureTime"] = "time without time zone NOT NULL",
                ["ArrivalTime"] = "time without time zone NOT NULL",
                ["ArrivalOffsetDays"] = "integer NOT NULL DEFAULT 0",
                ["IsActive"] = "boolean NOT NULL DEFAULT TRUE",
                ["CreatedAt"] = "timestamp with time zone NOT NULL",
                ["UpdatedAt"] = "timestamp with time zone NULL"
            }
        };
    }
}
