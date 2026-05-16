namespace API.Application.Dtos.Seeding;

public sealed record AirportSeedRecord(
    string Code,
    string Name,
    string City,
    string? Province,
    bool IsActive);

public sealed record AircraftSeedRecord(
    string AircraftCode,
    string Model,
    int TotalSeats,
    bool IsActive);

public sealed record SeatClassSeedRecord(
    string Code,
    string Name,
    decimal RefundPercent,
    decimal ChangeFee,
    int Priority);

public sealed record AircraftSeatTemplateSeedRecord(
    string AircraftCode,
    string SeatClassCode,
    int DefaultSeatCount,
    decimal DefaultBasePrice);

public sealed record RouteSeedRecord(
    string RouteCode,
    string DepartureAirportCode,
    string ArrivalAirportCode,
    int DistanceKm,
    int EstimatedDurationMinutes,
    bool IsActive);

public sealed record FlightDefinitionSeedRecord(
    string FlightDefinitionCode,
    string RouteCode,
    string AircraftCode,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    int? ArrivalOffsetDays,
    bool IsActive);

public sealed record WeeklyFlightTemplateSeedRecord(
    string TemplateCode,
    string TemplateName,
    string? Description,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string FlightDefinitionCode,
    int DayOfWeek,
    string? AircraftOverrideCode,
    TimeOnly? DepartureTimeOverride,
    TimeOnly? ArrivalTimeOverride,
    int? ArrivalOffsetDaysOverride,
    bool IsActive);
