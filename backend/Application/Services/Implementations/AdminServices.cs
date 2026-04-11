using FlightBooking.Application.DTOs.Admin;
using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.DTOs.Payment;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class AdminFlightService(IFlightRepository flightRepository, IAuditLogService auditLogService) : IAdminFlightService
{
    public async Task<AdminFlightSummaryDto> GetFlightSummaryAsync(Guid flightId)
    {
        var flight = await flightRepository.GetByIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        var totalSeats = flight.Inventories.Sum(i => i.TotalSeats);
        var soldSeats = flight.Inventories.Sum(i => i.SoldSeats);
        var availableSeats = flight.Inventories.Sum(i => i.AvailableSeats);
        var revenue = flight.FarePrices.Sum(p => p.CurrentPrice * soldSeats); // simplified

        return new AdminFlightSummaryDto(
            flight.Id, flight.FlightNumber,
            flight.Route?.OriginAirport?.Code ?? "",
            flight.Route?.DestinationAirport?.Code ?? "",
            flight.DepartureTime, flight.Status,
            totalSeats, soldSeats, availableSeats, revenue);
    }

    public async Task<IEnumerable<AdminFlightSummaryDto>> GetFlightsAsync(AdminFlightFilter filter)
    {
        var flights = await flightRepository.GetAllAsync(filter.Page, filter.PageSize);
        return (await Task.WhenAll(flights.Select(f => GetFlightSummaryAsync(f.Id)))).AsEnumerable();
    }

    public async Task BulkCancelFlightsAsync(IEnumerable<Guid> flightIds, string reason, Guid adminId)
    {
        foreach (var id in flightIds)
        {
            var flight = await flightRepository.GetByIdAsync(id);
            if (flight is null) continue;
            flight.Status = "cancelled";
            flight.UpdatedAt = DateTime.UtcNow;
        }
        await flightRepository.SaveChangesAsync();
        await auditLogService.LogAsync("bulk_cancel_flights", "Flight", null, null, new { Ids = flightIds, Reason = reason }, adminId);
    }

    public async Task AssignAircraftAsync(Guid flightId, Guid aircraftId, Guid adminId)
    {
        var flight = await flightRepository.GetByIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");
        flight.AircraftId = aircraftId;
        flight.UpdatedAt = DateTime.UtcNow;
        await flightRepository.SaveChangesAsync();
        await auditLogService.LogAsync("aircraft_assigned", "Flight", flightId.ToString(), null, new { AircraftId = aircraftId }, adminId);
    }
}

public class AdminPricingService(
    IPricingService pricingService,
    IPriceRuleRepository priceRuleRepository,
    IFlightFarePriceRepository farePriceRepository,
    IAuditLogService auditLogService) : IAdminPricingService
{
    public async Task<IEnumerable<PriceRuleDto>> GetPriceRulesAsync()
    {
        var rules = await priceRuleRepository.GetAllActiveAsync();
        return rules.Select(r => new PriceRuleDto(r.Id, r.RouteId, r.FareClassId, r.Season, r.BasePrice, r.Multiplier, r.IsActive));
    }

    public async Task<PriceRuleDto> CreatePriceRuleAsync(CreatePriceRuleRequest request, Guid adminId)
    {
        var rule = new Domain.Entities.PriceRule
        {
            Id = Guid.NewGuid(),
            RouteId = request.RouteId,
            FareClassId = request.FareClassId,
            Season = request.Season,
            DayOfWeek = request.DayOfWeek,
            DaysBeforeDeparture = request.DaysBeforeDeparture,
            BasePrice = request.BasePrice,
            Multiplier = request.Multiplier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await priceRuleRepository.AddAsync(rule);
        await priceRuleRepository.SaveChangesAsync();
        await auditLogService.LogAsync("price_rule_created", "PriceRule", rule.Id.ToString(), null, rule, adminId);
        return new PriceRuleDto(rule.Id, rule.RouteId, rule.FareClassId, rule.Season, rule.BasePrice, rule.Multiplier, rule.IsActive);
    }

    public Task<PriceRuleDto> UpdatePriceRuleAsync(Guid id, UpdatePriceRuleRequest request, Guid adminId)
        => throw new NotImplementedException("Requires PriceRule update implementation.");

    public Task DeletePriceRuleAsync(Guid id, Guid adminId)
        => throw new NotImplementedException("Requires PriceRule soft-delete implementation.");

    public Task<IEnumerable<PriceOverrideLogDto>> GetOverrideLogsAsync(Guid? flightId = null)
        => Task.FromResult(Enumerable.Empty<PriceOverrideLogDto>());

    public async Task TriggerPriceRecalculationAsync(Guid? flightId, Guid adminId)
    {
        if (flightId.HasValue)
        {
            await pricingService.RecalculateFlightPricesAsync(flightId.Value);
        }
        await auditLogService.LogAsync("price_recalculation_triggered", "Flight", flightId?.ToString(), null, null, adminId);
    }
}

public class AdminRefundService(
    IRefundService refundService,
    IRefundRepository refundRepository) : IAdminRefundService
{
    public async Task<IEnumerable<RefundDto>> GetPendingRefundsAsync(int page, int pageSize)
    {
        var refunds = await refundRepository.GetByStatusAsync("pending");
        return refunds.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new RefundDto(r.Id, r.PaymentId, r.BookingItemId, r.Amount, r.Reason, r.Status, r.GatewayRef, r.CreatedAt, r.UpdatedAt));
    }

    public Task<RefundDto> ApproveAsync(Guid refundId, Guid adminId)
        => refundService.ApproveRefundAsync(refundId, adminId);

    public Task<RefundDto> RejectAsync(Guid refundId, string reason, Guid adminId)
        => refundService.RejectRefundAsync(refundId, reason, adminId);

    public Task<AdminRefundStatsDto> GetRefundStatsAsync(DateOnly from, DateOnly to)
    {
        // In production: aggregate from DB
        return Task.FromResult(new AdminRefundStatsDto(from, to, 0, 0, 0, 0, 0));
    }
}
