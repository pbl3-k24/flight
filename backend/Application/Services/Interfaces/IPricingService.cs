using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IPricingService
{
    /// <summary>Calculate the current price for a given flight/fare class combination applying dynamic rules.</summary>
    Task<PriceBreakdownDto> CalculatePriceAsync(Guid flightId, Guid fareClassId, int passengerCount);

    /// <summary>Recalculate and persist prices for all active flights (called by scheduler or on rule change).</summary>
    Task RecalculateFlightPricesAsync(Guid flightId);

    /// <summary>Admin manually overrides the price for a specific flight/fare class.</summary>
    Task<PriceBreakdownDto> OverridePriceAsync(Guid flightId, Guid fareClassId, decimal newPrice, string reason, Guid adminId);
}
