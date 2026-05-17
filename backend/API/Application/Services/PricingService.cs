namespace API.Application.Services;

using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Common;
using Microsoft.Extensions.Logging;

public class PricingService : IPricingService
{
    private const decimal PriceRoundingStep = 10000m;
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
            var departureTime = inventory.Flight?.DepartureTime
                ?? throw new ValidationException("Flight departure time is required for dynamic pricing");
            var departureTimeVn = VietnamTime.ToVietnamTime(departureTime);
            var nowVn = VietnamTime.UtcNowInVietnam();

            var timeToDepartureMultiplier = GetTimeToDepartureMultiplier(departureTimeVn, nowVn);
            var flightTimeMultiplier = GetFlightTimeMultiplier(departureTimeVn);
            var dayOfWeekMultiplier = GetDayOfWeekMultiplier(departureTimeVn.DayOfWeek);

            var currentPrice = basePrice
                * timeToDepartureMultiplier
                * flightTimeMultiplier
                * dayOfWeekMultiplier;
            currentPrice = RoundToNearestPriceStep(currentPrice);

            _logger.LogInformation(
                "Calculated price for inventory {InventoryId}: Base={BasePrice}, DepartureMultiplier={DepartureMultiplier}, FlightTimeMultiplier={FlightTimeMultiplier}, DayOfWeekMultiplier={DayOfWeekMultiplier}, Final={FinalPrice}",
                flightSeatInventoryId, basePrice, timeToDepartureMultiplier, flightTimeMultiplier, dayOfWeekMultiplier, currentPrice);

            return currentPrice;
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

    private static decimal GetTimeToDepartureMultiplier(DateTime departureTime, DateTime nowUtc)
    {
        var timeToDeparture = departureTime - nowUtc;
        if (timeToDeparture.TotalHours < 6)
        {
            return 1.80m;
        }

        if (timeToDeparture.TotalHours < 24)
        {
            return 1.50m;
        }

        var days = timeToDeparture.TotalDays;
        if (days <= 2)
        {
            return 1.30m;
        }

        if (days <= 6)
        {
            return 1.15m;
        }

        if (days <= 14)
        {
            return 1.00m;
        }

        if (days <= 30)
        {
            return 0.95m;
        }

        return 0.85m;
    }

    private static decimal GetFlightTimeMultiplier(DateTime departureTime)
    {
        var time = departureTime.TimeOfDay;
        if (time < TimeSpan.FromHours(6))
        {
            return 0.85m;
        }

        if (time < TimeSpan.FromHours(9))
        {
            return 1.20m;
        }

        if (time < TimeSpan.FromHours(16))
        {
            return 1.00m;
        }

        if (time < TimeSpan.FromHours(20))
        {
            return 1.25m;
        }

        return 1.05m;
    }

    private static decimal GetDayOfWeekMultiplier(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 0.95m,
            DayOfWeek.Tuesday => 0.90m,
            DayOfWeek.Wednesday => 0.90m,
            DayOfWeek.Thursday => 1.00m,
            DayOfWeek.Friday => 1.15m,
            DayOfWeek.Saturday => 1.20m,
            DayOfWeek.Sunday => 1.15m,
            _ => 1.00m
        };
    }

    private static decimal RoundToNearestPriceStep(decimal price)
    {
        return Math.Round(price / PriceRoundingStep, MidpointRounding.AwayFromZero) * PriceRoundingStep;
    }
}
