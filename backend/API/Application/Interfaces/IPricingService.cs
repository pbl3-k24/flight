namespace API.Application.Interfaces;

public interface IPricingService
{
    /// <summary>
    /// Calculates the current dynamic price for a flight seat class.
    /// </summary>
    /// <param name="flightSeatInventoryId">Flight seat inventory ID</param>
    /// <returns>Current price based on demand and availability</returns>
    Task<decimal> CalculateCurrentPriceAsync(int flightSeatInventoryId);

    /// <summary>
    /// Updates dynamic prices for all active flights.
    /// </summary>
    Task UpdateDynamicPricesAsync();
}
