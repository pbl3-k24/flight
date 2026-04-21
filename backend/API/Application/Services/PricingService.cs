namespace API.Application.Services;

using API.Application.Exceptions;
using API.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class PricingService : IPricingService
{
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        IFlightSeatInventoryRepository seatInventoryRepository,
        IBookingRepository bookingRepository,
        ILogger<PricingService> logger)
    {
        _seatInventoryRepository = seatInventoryRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<decimal> CalculateCurrentPriceAsync(int flightSeatInventoryId)
    {
        try
        {
            // Get flight seat inventory
            var inventory = await _seatInventoryRepository.GetByIdAsync(flightSeatInventoryId);
            if (inventory == null)
            {
                throw new NotFoundException("Seat inventory not found");
            }

            var basePrice = inventory.BasePrice;

            // 1. Calculate occupancy factor (0.5x to 1.5x)
            var occupancyRatio = (decimal)(inventory.SoldSeats + inventory.HeldSeats) / inventory.TotalSeats;
            var occupancyFactor = occupancyRatio switch
            {
                < 0.3m => 0.7m,      // Low occupancy: 30% discount
                < 0.6m => 1.0m,      // Normal occupancy
                < 0.9m => 1.2m,      // High occupancy: 20% premium
                >= 0.9m => 1.5m      // Very high occupancy: 50% premium
            };

            // 2. Calculate time-based factor
            var daysUntilDeparture = (inventory.Flight.DepartureTime - DateTime.UtcNow).TotalDays;
            var timeFactor = daysUntilDeparture switch
            {
                <= 3 => 1.3m,        // Last-minute premium (30%)
                <= 7 => 1.2m,        // One week: 20% premium
                <= 14 => 1.0m,       // Two weeks: normal price
                > 14 => 0.8m         // Early booking: 20% discount
            };

            // 3. Calculate demand factor (1.0x to 1.15x)
            var demandFactor = await CalculateDemandFactorAsync(inventory.FlightId);

            // 4. Calculate final price
            var currentPrice = basePrice * occupancyFactor * timeFactor * demandFactor;

            // 5. Apply caps
            var minPrice = basePrice * 0.5m;  // Min: 50% of base price
            var maxPrice = basePrice * 2.0m;  // Max: 200% of base price

            currentPrice = Math.Max(minPrice, Math.Min(maxPrice, currentPrice));

            _logger.LogInformation(
                "Calculated price for inventory {InventoryId}: Base={BasePrice}, Occupancy={OccupancyFactor}, Time={TimeFactor}, Demand={DemandFactor}, Final={FinalPrice}",
                flightSeatInventoryId, basePrice, occupancyFactor, timeFactor, demandFactor, currentPrice);

            return Math.Round(currentPrice, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price for inventory {InventoryId}", flightSeatInventoryId);
            throw;
        }
    }

    public async Task UpdateDynamicPricesAsync()
    {
        try
        {
            _logger.LogInformation("Starting dynamic price update");

            // Get all active flights within next 30 days
            // This would query from flight repository
            // For each flight, update pricing for all seat classes

            // Example: Get all seat inventories and update prices
            var seatInventories = await _seatInventoryRepository.GetActiveInventoriesAsync();

            foreach (var inventory in seatInventories)
            {
                var newPrice = await CalculateCurrentPriceAsync(inventory.Id);
                var oldPrice = inventory.CurrentPrice;

                // Only update if change is significant (> 10%)
                var percentageChange = Math.Abs((newPrice - oldPrice) / oldPrice);
                if (percentageChange > 0.10m)
                {
                    inventory.CurrentPrice = newPrice;
                    await _seatInventoryRepository.UpdateAsync(inventory);

                    _logger.LogInformation(
                        "Updated price for flight {FlightId}, class {SeatClass}: {OldPrice} -> {NewPrice}",
                        inventory.FlightId, inventory.SeatClass.Name, oldPrice, newPrice);
                }
            }

            _logger.LogInformation("Dynamic price update completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dynamic prices");
            throw;
        }
    }

    private async Task<decimal> CalculateDemandFactorAsync(int flightId)
    {
        try
        {
            // Query recent bookings for this flight (last 7 days)
            var recentBookings = await _bookingRepository.GetRecentBookingsForFlightAsync(flightId, 7);

            // If high demand (many recent bookings), apply premium
            var bookingCount = recentBookings.Count();
            var demandFactor = bookingCount switch
            {
                >= 10 => 1.15m,      // High demand: 15% premium
                >= 5 => 1.10m,       // Moderate demand: 10% premium
                _ => 1.0m            // Normal demand
            };

            return demandFactor;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating demand factor for flight {FlightId}", flightId);
            return 1.0m; // Return normal if error
        }
    }
}
