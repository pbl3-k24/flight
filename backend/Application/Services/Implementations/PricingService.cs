using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class PricingService(
    IFlightFarePriceRepository farePriceRepository,
    IPriceRuleRepository priceRuleRepository,
    IFlightRepository flightRepository,
    IAuditLogService auditLogService) : IPricingService
{
    private const decimal TaxRate = 0.10m;
    private const decimal ServiceFee = 25000m;

    public async Task<PriceBreakdownDto> CalculatePriceAsync(Guid flightId, Guid fareClassId, int passengerCount)
    {
        var farePrice = await farePriceRepository.GetByFlightAndFareClassAsync(flightId, fareClassId)
            ?? throw new KeyNotFoundException($"No price found for flight {flightId} / fare class {fareClassId}.");

        var baseFare = farePrice.CurrentPrice;
        var tax = Math.Round(baseFare * TaxRate, 0);
        var total = baseFare + tax + ServiceFee;

        return new PriceBreakdownDto(flightId, fareClassId, baseFare, tax, ServiceFee, total, farePrice.PriceSource);
    }

    public async Task RecalculateFlightPricesAsync(Guid flightId)
    {
        var flight = await flightRepository.GetByIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        var rules = await priceRuleRepository.GetActiveRulesForRouteAsync(flight.RouteId);
        var farePrices = await farePriceRepository.GetByFlightAsync(flightId);

        foreach (var farePrice in farePrices)
        {
            if (farePrice.PriceSource == "manual")
                continue; // Skip admin overridden prices

            var applicableRule = rules
                .Where(r => r.FareClassId == null || r.FareClassId == farePrice.FareClassId)
                .OrderByDescending(r => r.FareClassId.HasValue) // prefer more specific rules
                .FirstOrDefault();

            if (applicableRule is null) continue;

            var daysUntilDeparture = (flight.DepartureTime - DateTime.UtcNow).Days;
            var urgencyMultiplier = daysUntilDeparture switch
            {
                <= 3 => 1.50m,
                <= 7 => 1.30m,
                <= 14 => 1.15m,
                <= 30 => 1.05m,
                _ => 1.00m
            };

            farePrice.CurrentPrice = Math.Round(applicableRule.BasePrice * applicableRule.Multiplier * urgencyMultiplier, 0);
            farePrice.PriceSource = "auto";
            farePrice.UpdatedAt = DateTime.UtcNow;
        }

        await farePriceRepository.SaveChangesAsync();
    }

    public async Task<PriceBreakdownDto> OverridePriceAsync(Guid flightId, Guid fareClassId, decimal newPrice, string reason, Guid adminId)
    {
        var farePrice = await farePriceRepository.GetByFlightAndFareClassAsync(flightId, fareClassId)
            ?? throw new KeyNotFoundException($"Price record not found.");

        var log = new PriceOverrideLog
        {
            Id = Guid.NewGuid(),
            FlightFarePriceId = farePrice.Id,
            AdminId = adminId,
            PriceBefore = farePrice.CurrentPrice,
            PriceAfter = newPrice,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        farePrice.CurrentPrice = newPrice;
        farePrice.PriceSource = "manual";
        farePrice.UpdatedAt = DateTime.UtcNow;
        farePrice.UpdatedBy = adminId;
        farePrice.OverrideLogs.Add(log);

        await farePriceRepository.SaveChangesAsync();
        await auditLogService.LogAsync("price_override", "FlightFarePrice", farePrice.Id.ToString(),
            new { PriceBefore = log.PriceBefore }, new { PriceAfter = newPrice, Reason = reason }, adminId);

        return await CalculatePriceAsync(flightId, fareClassId, 1);
    }
}
