namespace API.Application.Services;

using API.Application.Dtos.Flight;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class FlightService : IFlightService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IRouteRepository _routeRepository;
    private readonly IAirportRepository _airportRepository;
    private readonly IAircraftRepository _aircraftRepository;
    private readonly ISeatClassRepository _seatClassRepository;
    private readonly IPricingService _pricingService;
    private readonly IPromotionService _promotionService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<FlightService> _logger;

    public FlightService(
        IFlightRepository flightRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IRouteRepository routeRepository,
        IAirportRepository airportRepository,
        IAircraftRepository aircraftRepository,
        ISeatClassRepository seatClassRepository,
        IPricingService pricingService,
        IPromotionService promotionService,
        IDistributedCache cache,
        ILogger<FlightService> logger)
    {
        _flightRepository = flightRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _routeRepository = routeRepository;
        _airportRepository = airportRepository;
        _aircraftRepository = aircraftRepository;
        _seatClassRepository = seatClassRepository;
        _pricingService = pricingService;
        _promotionService = promotionService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<FlightSearchResponse>> SearchAsync(FlightSearchDto criteria)
    {
        try
        {
            // Validate input
            if (criteria.DepartureAirportId == criteria.ArrivalAirportId)
            {
                throw new ValidationException("Departure and arrival airports cannot be the same");
            }

            var departureDateVn = VietnamTime.GetVietnamDate(criteria.DepartureDate);
            if (departureDateVn < VietnamTime.UtcNowInVietnam().AddDays(-1).Date)
            {
                throw new ValidationException("Departure date cannot be in the past");
            }

            // Query flights for the route and date
            var flights = await _flightRepository.GetFlightsByRouteAndDateAsync(
                criteria.DepartureAirportId,
                criteria.ArrivalAirportId,
                VietnamTime.VietnamDayStartToUtc(departureDateVn),
                VietnamTime.VietnamDayEndExclusiveToUtc(departureDateVn));

            if (!string.IsNullOrWhiteSpace(criteria.FlightNumber))
            {
                var flightNumber = criteria.FlightNumber.Trim().ToUpperInvariant();
                flights = flights
                    .Where(f => string.Equals(f.FlightNumber, flightNumber, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var results = new List<FlightSearchResponse>();
            var nowVn = VietnamTime.UtcNowInVietnam();

            foreach (var flight in flights)
            {
                if (flight.Status != 0)
                {
                    continue;
                }

                if (VietnamTime.ToVietnamTime(flight.DepartureTime) <= nowVn)
                {
                    continue;
                }

                var response = new FlightSearchResponse
                {
                    FlightId = flight.Id,
                    FlightNumber = flight.FlightNumber,
                    DepartureAirport = flight.Route.DepartureAirport.Code,
                    ArrivalAirport = flight.Route.ArrivalAirport.Code,
                    DepartureTime = VietnamTime.ToVietnamTime(flight.DepartureTime),
                    ArrivalTime = VietnamTime.ToVietnamTime(flight.ArrivalTime),
                    DurationMinutes = flight.Route.EstimatedDurationMinutes,
                    AirlineCode = "FL", // Placeholder - should come from airline entity
                    AircraftModel = flight.Aircraft.Model,
                    AvailableSeatsByClass = [],
                    PricesByClass = []
                };

                // Get seat inventory for all classes
                var seatInventories = await _seatInventoryRepository.GetByFlightIdAsync(flight.Id);

                foreach (var inventory in seatInventories)
                {
                    var className = inventory.SeatClass.Name;
                    var availableSeats = inventory.AvailableSeats;
                    var currentPrice = await GetDynamicPriceAsync(inventory.Id);
                    var bookingPreviewPrice = BuildBookingPreviewPrice(currentPrice, criteria.PassengerCount);

                    // Filter out seat classes that don't have enough seats
                    if (criteria.PassengerCount > 0 && availableSeats < criteria.PassengerCount)
                    {
                        continue;
                    }

                    // Apply promotions if applicable
                    if (criteria.PassengerCount > 0)
                    {
                        bookingPreviewPrice = await _promotionService.ApplyPromotionAsync(bookingPreviewPrice, null); // Will add promotion ID param
                    }

                    response.AvailableSeatsByClass[className] = availableSeats;
                    response.PricesByClass[className] = bookingPreviewPrice;
                }

                // Filter by seat preference if specified
                if (criteria.SeatPreference.HasValue && response.PricesByClass.Count > 0)
                {
                    var preferredClass = await _seatClassRepository.GetByIdAsync(criteria.SeatPreference.Value);
                    if (preferredClass != null && response.AvailableSeatsByClass.ContainsKey(preferredClass.Name))
                    {
                        results.Add(response);
                    }
                }
                else if (response.PricesByClass.Count > 0)
                {
                    results.Add(response);
                }
            }

            // Sort by price (ascending)
            results = results.OrderBy(f => f.PricesByClass.Values.DefaultIfEmpty(decimal.MaxValue).Min()).ToList();

            _logger.LogInformation("Flight search completed: {Count} flights found", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flight search error");
            throw;
        }
    }

    public async Task<FlightDetailResponse> GetFlightAsync(int flightId)
    {
        try
        {
            // Check cache
            var cacheKey = $"flight-detail:{flightId}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<FlightDetailResponse>(cachedResult)
                    ?? throw new NotFoundException("Flight not found in cache");
            }

            // Get flight with related data
            var flight = await _flightRepository.GetByIdWithDetailsAsync(flightId);
            if (flight == null)
            {
                throw new NotFoundException("Flight not found");
            }

            var response = new FlightDetailResponse
            {
                FlightId = flight.Id,
                FlightNumber = flight.FlightNumber,
                DepartureTime = VietnamTime.ToVietnamTime(flight.DepartureTime),
                ArrivalTime = VietnamTime.ToVietnamTime(flight.ArrivalTime),
                DepartureAirport = flight.Route.DepartureAirport.Code,
                ArrivalAirport = flight.Route.ArrivalAirport.Code,
                DistanceKm = flight.Route.DistanceKm,
                DurationMinutes = flight.Route.EstimatedDurationMinutes,
                AircraftModel = flight.Aircraft.Model,
                SeatInventory = []
            };

            // Get seat inventory
            var seatInventories = await _seatInventoryRepository.GetByFlightIdAsync(flightId);
            foreach (var inventory in seatInventories)
            {
                response.SeatInventory[inventory.SeatClass.Name] = new SeatClassDetail
                {
                    SeatClassId = inventory.SeatClassId,
                    ClassName = inventory.SeatClass.Name,
                    TotalSeats = inventory.TotalSeats,
                    AvailableSeats = inventory.AvailableSeats,
                    HeldSeats = inventory.HeldSeats,
                    SoldSeats = inventory.SoldSeats,
                    CurrentPrice = await GetDynamicPriceAsync(inventory.Id),
                    BasePrice = inventory.BasePrice
                };
            }

            // Cache for 1 hour
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), cacheOptions);

            _logger.LogInformation("Flight details retrieved for flight {FlightId}", flightId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight details for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<int> GetAvailableSeatsAsync(int flightId, int seatClassId)
    {
        try
        {
            var seatInventory = await _seatInventoryRepository.GetByFlightAndSeatClassAsync(flightId, seatClassId);
            if (seatInventory == null)
            {
                throw new NotFoundException("Seat inventory not found");
            }

            // Release expired holds before returning count
            if (seatInventory.HeldSeats > 0)
            {
                // Check for expired bookings and release their seats
                // This would be handled by a background job in production
            }

            return seatInventory.AvailableSeats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available seats for flight {FlightId}", flightId);
            throw;
        }
    }

    public async Task<List<FlightSearchResponse>> GetFlightsByRouteAsync(int routeId, DateTime departureDate)
    {
        try
        {
            var flights = await _flightRepository.GetFlightsByRouteAndDateAsync(
                routeId,
                VietnamTime.VietnamDayStartToUtc(departureDate));

            var results = new List<FlightSearchResponse>();

            foreach (var flight in flights)
            {
                if (flight.Status != 0)
                {
                    continue;
                }

                var response = new FlightSearchResponse
                {
                    FlightId = flight.Id,
                    FlightNumber = flight.FlightNumber,
                    DepartureAirport = flight.Route.DepartureAirport.Code,
                    ArrivalAirport = flight.Route.ArrivalAirport.Code,
                    DepartureTime = VietnamTime.ToVietnamTime(flight.DepartureTime),
                    ArrivalTime = VietnamTime.ToVietnamTime(flight.ArrivalTime),
                    DurationMinutes = flight.Route.EstimatedDurationMinutes,
                    AirlineCode = "FL",
                    AircraftModel = flight.Aircraft.Model,
                    AvailableSeatsByClass = [],
                    PricesByClass = []
                };

                var seatInventories = await _seatInventoryRepository.GetByFlightIdAsync(flight.Id);
                foreach (var inventory in seatInventories)
                {
                    response.AvailableSeatsByClass[inventory.SeatClass.Name] = inventory.AvailableSeats;
                    response.PricesByClass[inventory.SeatClass.Name] = await GetDynamicPriceAsync(inventory.Id);
                }

                results.Add(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flights for route {RouteId}", routeId);
            throw;
        }
    }

    private static decimal BuildBookingPreviewPrice(decimal unitPrice, int passengerCount)
    {
        var count = passengerCount > 0 ? passengerCount : 1;
        return unitPrice * count;
    }

    private async Task<decimal> GetDynamicPriceAsync(int flightSeatInventoryId)
    {
        return await _pricingService.CalculateCurrentPriceAsync(flightSeatInventoryId);
    }
}
