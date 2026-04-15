namespace API.Application.Services;

using API.Application.Dtos.Flight;
using API.Application.Interfaces;
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

            if (criteria.DepartureDate < DateTime.UtcNow.Date)
            {
                throw new ValidationException("Departure date cannot be in the past");
            }

            // Check cache
            var cacheKey = $"flight-search:{criteria.DepartureAirportId}:{criteria.ArrivalAirportId}:{criteria.DepartureDate:yyyy-MM-dd}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                _logger.LogInformation("Returning cached flight search results");
                return JsonSerializer.Deserialize<List<FlightSearchResponse>>(cachedResult) ?? [];
            }

            // Query flights for the route and date
            var flights = await _flightRepository.GetFlightsByRouteAndDateAsync(
                criteria.DepartureAirportId,
                criteria.ArrivalAirportId,
                criteria.DepartureDate,
                criteria.DepartureDate.AddDays(1)); // Allow ±1 day for flexibility

            var results = new List<FlightSearchResponse>();

            foreach (var flight in flights)
            {
                var response = new FlightSearchResponse
                {
                    FlightId = flight.Id,
                    FlightNumber = flight.FlightNumber,
                    DepartureAirport = flight.Route.DepartureAirport.Code,
                    ArrivalAirport = flight.Route.ArrivalAirport.Code,
                    DepartureTime = flight.DepartureTime,
                    ArrivalTime = flight.ArrivalTime,
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
                    var currentPrice = inventory.CurrentPrice;

                    // Apply promotions if applicable
                    if (criteria.PassengerCount > 0)
                    {
                        currentPrice = await _promotionService.ApplyPromotionAsync(currentPrice, null); // Will add promotion ID param
                    }

                    response.AvailableSeatsByClass[className] = availableSeats;
                    response.PricesByClass[className] = currentPrice;
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
                else
                {
                    results.Add(response);
                }
            }

            // Sort by price (ascending)
            results = results.OrderBy(f => f.PricesByClass.Values.DefaultIfEmpty(decimal.MaxValue).Min()).ToList();

            // Cache results for 30 minutes
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(results), cacheOptions);

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
                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
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
                    CurrentPrice = inventory.CurrentPrice,
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
                departureDate);

            var results = new List<FlightSearchResponse>();

            foreach (var flight in flights)
            {
                var response = new FlightSearchResponse
                {
                    FlightId = flight.Id,
                    FlightNumber = flight.FlightNumber,
                    DepartureAirport = flight.Route.DepartureAirport.Code,
                    ArrivalAirport = flight.Route.ArrivalAirport.Code,
                    DepartureTime = flight.DepartureTime,
                    ArrivalTime = flight.ArrivalTime,
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
                    response.PricesByClass[inventory.SeatClass.Name] = inventory.CurrentPrice;
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
}
