namespace API.Infrastructure.Services;

using API.Application.Dtos.Seeding;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

public class CsvSeedService : ICsvSeedService
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<CsvSeedService> _logger;

    public CsvSeedService(FlightBookingDbContext context, ILogger<CsvSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CsvSeedResult> SeedAsync(string seedDirectory, CancellationToken cancellationToken = default)
    {
        var result = new CsvSeedResult();
        if (!Directory.Exists(seedDirectory))
        {
            result.Messages.Add($"CSV seed directory not found: {seedDirectory}. Skipping CSV seed.");
            _logger.LogInformation("CSV seed directory not found: {SeedDirectory}. Skipping CSV seed.", seedDirectory);
            return result;
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            await SeedAirportsAsync(seedDirectory, result, cancellationToken);
            await SeedAircraftAsync(seedDirectory, result, cancellationToken);
            await SeedSeatClassesAsync(seedDirectory, result, cancellationToken);
            await SeedAircraftSeatTemplatesAsync(seedDirectory, result, cancellationToken);
            await SeedRoutesAsync(seedDirectory, result, cancellationToken);
            await SeedFlightDefinitionsAsync(seedDirectory, result, cancellationToken);
            await SeedWeeklyTemplatesAsync(seedDirectory, result, cancellationToken);

            if (result.HasErrors)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            await transaction.CommitAsync(cancellationToken);
        });

        if (result.HasErrors)
        {
            foreach (var error in result.Errors)
            {
                _logger.LogError("CSV seed error: {Error}", error);
            }
            return result;
        }

        foreach (var message in result.Messages)
        {
            _logger.LogInformation("{Message}", message);
        }

        return result;
    }

    private async Task SeedAirportsAsync(string seedDirectory, CsvSeedResult result, CancellationToken cancellationToken)
    {
        var records = ReadCsv(Path.Combine(seedDirectory, "airports.csv"), result, ParseAirport);
        if (records.Count == 0) return;

        var existingCodes = (await _context.Airports
            .Select(a => a.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        foreach (var record in records)
        {
            if (existingCodes.Contains(record.Code))
            {
                result.Skipped++;
                continue;
            }

            _context.Airports.Add(new Airport
            {
                Code = record.Code,
                Name = record.Name,
                City = record.City,
                Province = record.Province,
                IsActive = record.IsActive,
                IsDeleted = false
            });
            existingCodes.Add(record.Code);
            inserted++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        result.Inserted += inserted;
        result.Messages.Add($"airports.csv: inserted={inserted}, skipped={records.Count - inserted}");
    }

    private async Task SeedAircraftAsync(string seedDirectory, CsvSeedResult result, CancellationToken cancellationToken)
    {
        var records = ReadCsv(Path.Combine(seedDirectory, "aircrafts.csv"), result, ParseAircraft);
        if (records.Count == 0) return;

        var existingCodes = (await _context.Aircraft
            .Select(a => a.RegistrationNumber)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        foreach (var record in records)
        {
            if (existingCodes.Contains(record.AircraftCode))
            {
                result.Skipped++;
                continue;
            }

            _context.Aircraft.Add(new Aircraft
            {
                RegistrationNumber = record.AircraftCode,
                Model = record.Model,
                TotalSeats = record.TotalSeats,
                IsActive = record.IsActive,
                IsDeleted = false
            });
            existingCodes.Add(record.AircraftCode);
            inserted++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        result.Inserted += inserted;
        result.Messages.Add($"aircrafts.csv: inserted={inserted}, skipped={records.Count - inserted}");
    }

    private async Task SeedSeatClassesAsync(string seedDirectory, CsvSeedResult result, CancellationToken cancellationToken)
    {
        var records = ReadCsv(Path.Combine(seedDirectory, "seat_classes.csv"), result, ParseSeatClass);
        if (records.Count == 0) return;

        var existingCodes = (await _context.SeatClasses
            .Select(s => s.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        foreach (var record in records)
        {
            if (existingCodes.Contains(record.Code))
            {
                result.Skipped++;
                continue;
            }

            _context.SeatClasses.Add(new SeatClass
            {
                Code = record.Code,
                Name = record.Name,
                RefundPercent = record.RefundPercent,
                ChangeFee = record.ChangeFee,
                Priority = record.Priority,
                IsDeleted = false
            });
            existingCodes.Add(record.Code);
            inserted++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        result.Inserted += inserted;
        result.Messages.Add($"seat_classes.csv: inserted={inserted}, skipped={records.Count - inserted}");
    }

    private async Task SeedAircraftSeatTemplatesAsync(string seedDirectory, CsvSeedResult result, CancellationToken cancellationToken)
    {
        var records = ReadCsv(Path.Combine(seedDirectory, "aircraft_seat_templates.csv"), result, ParseAircraftSeatTemplate);
        if (records.Count == 0) return;

        var aircraftByCode = (await _context.Aircraft.ToListAsync(cancellationToken))
            .ToDictionary(a => a.RegistrationNumber, StringComparer.OrdinalIgnoreCase);
        var seatClassByCode = (await _context.SeatClasses.ToListAsync(cancellationToken))
            .ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);
        var existing = await _context.AircraftSeatTemplates
            .Select(t => new { t.AircraftId, t.SeatClassId })
            .ToListAsync(cancellationToken);
        var existingKeys = existing.Select(x => $"{x.AircraftId}:{x.SeatClassId}").ToHashSet();

        var inserted = 0;
        foreach (var record in records)
        {
            if (!aircraftByCode.TryGetValue(record.AircraftCode, out var aircraft))
            {
                result.Errors.Add($"aircraft_seat_templates.csv: AircraftCode '{record.AircraftCode}' does not exist");
                continue;
            }

            if (!seatClassByCode.TryGetValue(record.SeatClassCode, out var seatClass))
            {
                result.Errors.Add($"aircraft_seat_templates.csv: SeatClassCode '{record.SeatClassCode}' does not exist");
                continue;
            }

            var key = $"{aircraft.Id}:{seatClass.Id}";
            if (existingKeys.Contains(key))
            {
                result.Skipped++;
                continue;
            }

            _context.AircraftSeatTemplates.Add(new AircraftSeatTemplate
            {
                AircraftId = aircraft.Id,
                SeatClassId = seatClass.Id,
                DefaultSeatCount = record.DefaultSeatCount,
                DefaultBasePrice = record.DefaultBasePrice,
                IsDeleted = false
            });
            existingKeys.Add(key);
            inserted++;
        }

        ValidateAircraftSeatTotals(records, aircraftByCode, result);
        if (result.HasErrors) return;

        await _context.SaveChangesAsync(cancellationToken);
        result.Inserted += inserted;
        result.Messages.Add($"aircraft_seat_templates.csv: inserted={inserted}, skipped={records.Count - inserted}");
    }

    private async Task SeedRoutesAsync(string seedDirectory, CsvSeedResult result, CancellationToken cancellationToken)
    {
        var records = ReadCsv(Path.Combine(seedDirectory, "routes.csv"), result, ParseRoute);
        if (records.Count == 0) return;

        var airportByCode = (await _context.Airports.ToListAsync(cancellationToken))
            .ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase);
        var existingRoutes = await _context.Routes.ToListAsync(cancellationToken);
        var existingCodes = existingRoutes
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .Select(r => r.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        foreach (var record in records)
        {
            if (record.DepartureAirportCode.Equals(record.ArrivalAirportCode, StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"routes.csv: RouteCode '{record.RouteCode}' has the same departure and arrival airport");
                continue;
            }

            if (!airportByCode.TryGetValue(record.DepartureAirportCode, out var departureAirport))
            {
                result.Errors.Add($"routes.csv: DepartureAirportCode '{record.DepartureAirportCode}' does not exist");
                continue;
            }

            if (!airportByCode.TryGetValue(record.ArrivalAirportCode, out var arrivalAirport))
            {
                result.Errors.Add($"routes.csv: ArrivalAirportCode '{record.ArrivalAirportCode}' does not exist");
                continue;
            }

            if (existingCodes.Contains(record.RouteCode))
            {
                result.Skipped++;
                continue;
            }

            var existingRoute = existingRoutes.FirstOrDefault(r =>
                r.DepartureAirportId == departureAirport.Id &&
                r.ArrivalAirportId == arrivalAirport.Id &&
                string.IsNullOrWhiteSpace(r.Code));
            if (existingRoute != null)
            {
                existingRoute.Code = record.RouteCode;
                existingRoute.DistanceKm = record.DistanceKm;
                existingRoute.EstimatedDurationMinutes = record.EstimatedDurationMinutes;
                existingRoute.IsActive = record.IsActive;
                existingCodes.Add(record.RouteCode);
                result.Skipped++;
                continue;
            }

            _context.Routes.Add(new Route
            {
                Code = record.RouteCode,
                DepartureAirportId = departureAirport.Id,
                ArrivalAirportId = arrivalAirport.Id,
                DistanceKm = record.DistanceKm,
                EstimatedDurationMinutes = record.EstimatedDurationMinutes,
                IsActive = record.IsActive,
                IsDeleted = false
            });
            existingCodes.Add(record.RouteCode);
            inserted++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        result.Inserted += inserted;
        result.Messages.Add($"routes.csv: inserted={inserted}, skipped={records.Count - inserted}");
    }

    private async Task SeedFlightDefinitionsAsync(string seedDirectory, CsvSeedResult result, CancellationToken cancellationToken)
    {
        var records = ReadCsv(Path.Combine(seedDirectory, "flight_definitions.csv"), result, ParseFlightDefinition);
        if (records.Count == 0) return;

        var routeByCode = (await _context.Routes.ToListAsync(cancellationToken))
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);
        var aircraftByCode = (await _context.Aircraft.ToListAsync(cancellationToken))
            .ToDictionary(a => a.RegistrationNumber, StringComparer.OrdinalIgnoreCase);
        var existingCodes = (await _context.FlightDefinitions
            .Select(f => f.FlightNumber)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        foreach (var record in records)
        {
            if (!routeByCode.TryGetValue(record.RouteCode, out var route))
            {
                result.Errors.Add($"flight_definitions.csv: RouteCode '{record.RouteCode}' does not exist");
                continue;
            }

            if (!aircraftByCode.TryGetValue(record.AircraftCode, out var aircraft))
            {
                result.Errors.Add($"flight_definitions.csv: AircraftCode '{record.AircraftCode}' does not exist");
                continue;
            }

            var offsetDays = ResolveArrivalOffsetDays(record.DepartureTime, record.ArrivalTime, record.ArrivalOffsetDays, result,
                $"flight_definitions.csv: FlightDefinitionCode '{record.FlightDefinitionCode}'");
            if (offsetDays == null)
            {
                continue;
            }

            var duplicatePattern = await _context.FlightDefinitions.AnyAsync(fd =>
                fd.RouteId == route.Id &&
                fd.DefaultAircraftId == aircraft.Id &&
                fd.FlightNumber == record.FlightDefinitionCode,
                cancellationToken);
            if (existingCodes.Contains(record.FlightDefinitionCode) || duplicatePattern)
            {
                result.Skipped++;
                continue;
            }

            _context.FlightDefinitions.Add(new FlightDefinition
            {
                FlightNumber = record.FlightDefinitionCode,
                RouteId = route.Id,
                DefaultAircraftId = aircraft.Id,
                DepartureTime = record.DepartureTime,
                ArrivalTime = record.ArrivalTime,
                ArrivalOffsetDays = offsetDays.Value,
                IsActive = record.IsActive,
                CreatedAt = DateTime.UtcNow
            });
            existingCodes.Add(record.FlightDefinitionCode);
            inserted++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        result.Inserted += inserted;
        result.Messages.Add($"flight_definitions.csv: inserted={inserted}, skipped={records.Count - inserted}");
    }

    private async Task SeedWeeklyTemplatesAsync(string seedDirectory, CsvSeedResult result, CancellationToken cancellationToken)
    {
        var records = ReadCsv(Path.Combine(seedDirectory, "weekly_flight_templates.csv"), result, ParseWeeklyTemplate);
        if (records.Count == 0) return;

        var definitionsByCode = (await _context.FlightDefinitions.ToListAsync(cancellationToken))
            .ToDictionary(fd => fd.FlightNumber, StringComparer.OrdinalIgnoreCase);
        var aircraftByCode = (await _context.Aircraft.ToListAsync(cancellationToken))
            .ToDictionary(a => a.RegistrationNumber, StringComparer.OrdinalIgnoreCase);
        var templatesByCode = (await _context.FlightScheduleTemplates.ToListAsync(cancellationToken))
            .Where(t => !string.IsNullOrWhiteSpace(t.Code))
            .ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        var existingDetails = await _context.FlightTemplateDetails
            .Include(d => d.FlightDefinition)
            .ToListAsync(cancellationToken);
        var inserted = 0;

        foreach (var group in records.GroupBy(r => r.TemplateCode, StringComparer.OrdinalIgnoreCase))
        {
            var first = group.First();
            if (!templatesByCode.TryGetValue(first.TemplateCode, out var template))
            {
                template = new FlightScheduleTemplate
                {
                    Code = first.TemplateCode,
                    Name = first.TemplateName,
                    Description = first.Description,
                    EffectiveFrom = first.EffectiveFrom,
                    EffectiveTo = first.EffectiveTo,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.FlightScheduleTemplates.Add(template);
                await _context.SaveChangesAsync(cancellationToken);
                templatesByCode.Add(template.Code, template);
                inserted++;
            }

            foreach (var record in group)
            {
                if (record.DayOfWeek < 0 || record.DayOfWeek > 6)
                {
                    result.Errors.Add($"weekly_flight_templates.csv: DayOfWeek must be 0..6 for TemplateCode '{record.TemplateCode}'");
                    continue;
                }

                if (!definitionsByCode.TryGetValue(record.FlightDefinitionCode, out var definition))
                {
                    result.Errors.Add($"weekly_flight_templates.csv: FlightDefinitionCode '{record.FlightDefinitionCode}' does not exist");
                    continue;
                }

                int? aircraftOverrideId = null;
                if (!string.IsNullOrWhiteSpace(record.AircraftOverrideCode))
                {
                    if (!aircraftByCode.TryGetValue(record.AircraftOverrideCode, out var aircraftOverride))
                    {
                        result.Errors.Add($"weekly_flight_templates.csv: AircraftOverrideCode '{record.AircraftOverrideCode}' does not exist");
                        continue;
                    }
                    aircraftOverrideId = aircraftOverride.Id;
                }

                var departure = record.DepartureTimeOverride ?? definition.DepartureTime;
                var arrival = record.ArrivalTimeOverride ?? definition.ArrivalTime;
                var offset = ResolveArrivalOffsetDays(departure, arrival, record.ArrivalOffsetDaysOverride ?? definition.ArrivalOffsetDays, result,
                    $"weekly_flight_templates.csv: TemplateCode '{record.TemplateCode}', FlightDefinitionCode '{record.FlightDefinitionCode}'");
                if (offset == null)
                {
                    continue;
                }

                var exists = existingDetails.Any(d =>
                    d.TemplateId == template.Id &&
                    d.FlightDefinitionId == definition.Id &&
                    d.DayOfWeek == record.DayOfWeek &&
                    (d.DepartureTimeOverride ?? d.FlightDefinition.DepartureTime) == departure);
                if (exists)
                {
                    result.Skipped++;
                    continue;
                }

                var detail = new FlightTemplateDetail
                {
                    TemplateId = template.Id,
                    FlightDefinitionId = definition.Id,
                    DayOfWeek = record.DayOfWeek,
                    AircraftOverrideId = aircraftOverrideId,
                    DepartureTimeOverride = record.DepartureTimeOverride,
                    ArrivalTimeOverride = record.ArrivalTimeOverride,
                    ArrivalOffsetDaysOverride = record.ArrivalOffsetDaysOverride,
                    IsActive = record.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                _context.FlightTemplateDetails.Add(detail);
                existingDetails.Add(detail);
                inserted++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        result.Inserted += inserted;
        result.Messages.Add($"weekly_flight_templates.csv: inserted={inserted}, skipped={records.Count - inserted}");
    }

    private static void ValidateAircraftSeatTotals(
        List<AircraftSeatTemplateSeedRecord> records,
        Dictionary<string, Aircraft> aircraftByCode,
        CsvSeedResult result)
    {
        foreach (var group in records.GroupBy(r => r.AircraftCode, StringComparer.OrdinalIgnoreCase))
        {
            if (!aircraftByCode.TryGetValue(group.Key, out var aircraft))
            {
                continue;
            }

            var total = group.Sum(r => r.DefaultSeatCount);
            if (total != aircraft.TotalSeats)
            {
                result.Errors.Add($"aircraft_seat_templates.csv: AircraftCode '{group.Key}' seat template total {total} does not match Aircraft.TotalSeats {aircraft.TotalSeats}");
            }
        }
    }

    private static int? ResolveArrivalOffsetDays(TimeOnly departure, TimeOnly arrival, int? configuredOffset, CsvSeedResult result, string context)
    {
        var offset = configuredOffset ?? (arrival < departure ? 1 : 0);
        if (offset < 0 || offset > 2)
        {
            result.Errors.Add($"{context}: ArrivalOffsetDays must be between 0 and 2");
            return null;
        }

        if (arrival < departure && offset == 0)
        {
            result.Errors.Add($"{context}: ArrivalOffsetDays must be greater than 0 when ArrivalTime is earlier than DepartureTime");
            return null;
        }

        if (arrival == departure && offset == 0)
        {
            result.Errors.Add($"{context}: ArrivalTime must differ from DepartureTime unless ArrivalOffsetDays is greater than 0");
            return null;
        }

        return offset;
    }

    private static List<T> ReadCsv<T>(string path, CsvSeedResult result, Func<Dictionary<string, string>, int, T> parser)
    {
        if (!File.Exists(path))
        {
            result.Messages.Add($"{Path.GetFileName(path)}: file not found, skipped");
            return [];
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length <= 1)
        {
            result.Messages.Add($"{Path.GetFileName(path)}: empty file, skipped");
            return [];
        }

        var headers = SplitCsvLine(lines[0]).Select(h => h.Trim()).ToList();
        var records = new List<T>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            try
            {
                var values = SplitCsvLine(lines[i]);
                if (values.Count != headers.Count)
                {
                    result.Errors.Add($"{Path.GetFileName(path)} line {i + 1}: column count does not match header count");
                    continue;
                }

                var row = headers
                    .Select((h, index) => new { h, value = values[index].Trim() })
                    .ToDictionary(x => x.h, x => x.value, StringComparer.OrdinalIgnoreCase);

                records.Add(parser(row, i + 1));
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{Path.GetFileName(path)} line {i + 1}: {ex.Message}");
            }
        }

        return records;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values;
    }

    private static AirportSeedRecord ParseAirport(Dictionary<string, string> row, int line) => new(
        Required(row, "Code", line).ToUpperInvariant(),
        Required(row, "Name", line),
        Required(row, "City", line),
        Optional(row, "Province"),
        Bool(row, "IsActive", true, line));

    private static AircraftSeedRecord ParseAircraft(Dictionary<string, string> row, int line) => new(
        Required(row, "AircraftCode", line).ToUpperInvariant(),
        Required(row, "Model", line),
        PositiveInt(row, "TotalSeats", line),
        Bool(row, "IsActive", true, line));

    private static SeatClassSeedRecord ParseSeatClass(Dictionary<string, string> row, int line) => new(
        Required(row, "Code", line).ToUpperInvariant(),
        Required(row, "Name", line),
        Decimal(row, "RefundPercent", line),
        Decimal(row, "ChangeFee", line),
        PositiveInt(row, "Priority", line));

    private static AircraftSeatTemplateSeedRecord ParseAircraftSeatTemplate(Dictionary<string, string> row, int line) => new(
        Required(row, "AircraftCode", line).ToUpperInvariant(),
        Required(row, "SeatClassCode", line).ToUpperInvariant(),
        PositiveInt(row, "DefaultSeatCount", line),
        Decimal(row, "DefaultBasePrice", line));

    private static RouteSeedRecord ParseRoute(Dictionary<string, string> row, int line) => new(
        Required(row, "RouteCode", line).ToUpperInvariant(),
        Required(row, "DepartureAirportCode", line).ToUpperInvariant(),
        Required(row, "ArrivalAirportCode", line).ToUpperInvariant(),
        PositiveInt(row, "DistanceKm", line),
        PositiveInt(row, "EstimatedDurationMinutes", line),
        Bool(row, "IsActive", true, line));

    private static FlightDefinitionSeedRecord ParseFlightDefinition(Dictionary<string, string> row, int line) => new(
        Required(row, "FlightDefinitionCode", line).ToUpperInvariant(),
        Required(row, "RouteCode", line).ToUpperInvariant(),
        Required(row, "AircraftCode", line).ToUpperInvariant(),
        Time(row, "DepartureTime", line),
        Time(row, "ArrivalTime", line),
        OptionalInt(row, "ArrivalOffsetDays", line),
        Bool(row, "IsActive", true, line));

    private static WeeklyFlightTemplateSeedRecord ParseWeeklyTemplate(Dictionary<string, string> row, int line) => new(
        Required(row, "TemplateCode", line).ToUpperInvariant(),
        Required(row, "TemplateName", line),
        Optional(row, "Description"),
        OptionalDate(row, "EffectiveFrom", line),
        OptionalDate(row, "EffectiveTo", line),
        Required(row, "FlightDefinitionCode", line).ToUpperInvariant(),
        Int(row, "DayOfWeek", line),
        Optional(row, "AircraftOverrideCode")?.ToUpperInvariant(),
        OptionalTime(row, "DepartureTimeOverride", line),
        OptionalTime(row, "ArrivalTimeOverride", line),
        OptionalInt(row, "ArrivalOffsetDaysOverride", line),
        Bool(row, "IsActive", true, line));

    private static string Required(Dictionary<string, string> row, string column, int line)
    {
        if (!row.TryGetValue(column, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"line {line}: required column '{column}' is missing or empty");
        }

        return value.Trim();
    }

    private static string? Optional(Dictionary<string, string> row, string column)
    {
        return row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static int Int(Dictionary<string, string> row, string column, int line)
    {
        var raw = Required(row, column, line);
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"line {line}: column '{column}' must be an integer");
        }
        return value;
    }

    private static int PositiveInt(Dictionary<string, string> row, string column, int line)
    {
        var value = Int(row, column, line);
        if (value <= 0)
        {
            throw new FormatException($"line {line}: column '{column}' must be greater than 0");
        }
        return value;
    }

    private static int? OptionalInt(Dictionary<string, string> row, string column, int line)
    {
        var raw = Optional(row, column);
        if (raw == null) return null;
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"line {line}: column '{column}' must be an integer");
        }
        return value;
    }

    private static decimal Decimal(Dictionary<string, string> row, string column, int line)
    {
        var raw = Required(row, column, line);
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"line {line}: column '{column}' must be a decimal");
        }
        return value;
    }

    private static bool Bool(Dictionary<string, string> row, string column, bool defaultValue, int line)
    {
        var raw = Optional(row, column);
        if (raw == null) return defaultValue;
        if (bool.TryParse(raw, out var value)) return value;
        if (raw == "1") return true;
        if (raw == "0") return false;
        throw new FormatException($"line {line}: column '{column}' must be true/false");
    }

    private static TimeOnly Time(Dictionary<string, string> row, string column, int line)
    {
        var raw = Required(row, column, line);
        if (!TimeOnly.TryParseExact(raw, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            throw new FormatException($"line {line}: column '{column}' must use HH:mm format");
        }
        return value;
    }

    private static TimeOnly? OptionalTime(Dictionary<string, string> row, string column, int line)
    {
        var raw = Optional(row, column);
        if (raw == null) return null;
        if (!TimeOnly.TryParseExact(raw, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            throw new FormatException($"line {line}: column '{column}' must use HH:mm format");
        }
        return value;
    }

    private static DateOnly? OptionalDate(Dictionary<string, string> row, string column, int line)
    {
        var raw = Optional(row, column);
        if (raw == null) return null;
        if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            throw new FormatException($"line {line}: column '{column}' must use yyyy-MM-dd format");
        }
        return value;
    }
}
