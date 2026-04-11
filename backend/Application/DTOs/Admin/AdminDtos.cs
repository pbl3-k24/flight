namespace FlightBooking.Application.DTOs.Admin;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityType,
    string? EntityId,
    string? Before,
    string? After,
    string? IpAddress,
    DateTime CreatedAt);

public record AuditLogFilter(
    Guid? UserId = null,
    string? Action = null,
    string? EntityType = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50);

public record AdminFlightSummaryDto(
    Guid Id,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    string Status,
    int TotalSeats,
    int SoldSeats,
    int AvailableSeats,
    decimal Revenue);

public record AdminFlightFilter(
    string? Status = null,
    Guid? RouteId = null,
    DateOnly? DepartureDateFrom = null,
    DateOnly? DepartureDateTo = null,
    int Page = 1,
    int PageSize = 50);

public record AdminRefundStatsDto(
    DateOnly From,
    DateOnly To,
    int TotalRequests,
    int Approved,
    int Rejected,
    int Pending,
    decimal TotalRefundedAmount);
