namespace API.Application.Services;

using API.Application.DTOs;
using API.Application.Interfaces;
using API.Domain.Entities;
using API.Domain.Exceptions;

/// <summary>
/// Service for managing flight operations.
/// Implements IFlightService with business logic, validation, and caching strategies.
/// Note: Manual mapping is used. AutoMapper can be integrated later.
/// </summary>
public class FlightService : IFlightService
{
    private readonly IFlightRepository _flightRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<FlightService> _logger;

    // Cache key constants
    private const string FLIGHT_CACHE_KEY = "flight_{0}";
    private const string FLIGHTS_SEARCH_CACHE_KEY = "flights_search_{0}_{1}_{2}";
    private const string FLIGHTS_CACHE_PATTERN = "flight_*";

    // Cache TTL constants
    private static readonly TimeSpan FLIGHT_CACHE_TTL = TimeSpan.FromHours(1);
    private static readonly TimeSpan SEARCH_CACHE_TTL = TimeSpan.FromMinutes(30);

    public FlightService(
        IFlightRepository flightRepository,
        ICacheService cacheService,
        ILogger<FlightService> logger)
    {
        _flightRepository = flightRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a flight by ID with caching support.
    /// Algorithm:
    /// 1. Check cache with key "flight_{id}"
    /// 2. If cached, return immediately
    /// 3. Query repository by ID
    /// 4. If not found, throw FlightNotFoundException
    /// 5. Map entity to ResponseDto
    /// 6. Cache result with 1-hour TTL
    /// 7. Log successful retrieval
    /// 8. Return ResponseDto
    /// </summary>
    public async Task<FlightResponseDto> GetFlightAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Flight ID must be greater than 0.");

        _logger.LogInformation("Fetching flight with ID: {FlightId}", id);

        try
        {
            // 1. Check cache
            var cacheKey = string.Format(FLIGHT_CACHE_KEY, id);
            var cachedFlight = await _cacheService.GetAsync<FlightResponseDto>(cacheKey);
            
            if (cachedFlight != null)
            {
                _logger.LogInformation("Flight {FlightId} found in cache", id);
                return cachedFlight;
            }

            // 2. Query repository
            var flight = await _flightRepository.GetByIdAsync(id);
            
            // 3. Check if found
            if (flight == null)
            {
                _logger.LogWarning("Flight with ID {FlightId} not found", id);
                throw new FlightNotFoundException(id);
            }

            // 4. Map to DTO
            var responseDto = MapFlightToResponseDto(flight);

            // 5. Cache result with 1-hour TTL
            await _cacheService.SetAsync(cacheKey, responseDto, FLIGHT_CACHE_TTL);

            // 6. Log success
            _logger.LogInformation("Successfully retrieved flight {FlightId}: {FlightNumber}", id, flight.FlightNumber);

            // 7. Return DTO
            return responseDto;
        }
        catch (FlightNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight with ID: {FlightId}", id);
            throw;
        }
    }

    /// <summary>
    /// Searches for flights matching specified criteria.
    /// Algorithm:
    /// 1. Validate search criteria
    /// 2. Generate cache key
    /// 3. Check cache for search results
    /// 4. If cached, return immediately
    /// 5. Query repository with filters
    /// 6. Map entities to DTOs
    /// 7. Cache results with 30-minute TTL
    /// 8. Log search operation
    /// 9. Return results
    /// </summary>
    public async Task<IEnumerable<FlightResponseDto>> SearchFlightsAsync(FlightSearchDto criteria)
    {
        if (criteria == null)
            throw new ValidationException("Search criteria cannot be null.");

        if (criteria.DepartureAirportId <= 0)
            throw new ValidationException("Departure airport must be valid.");

        if (criteria.ArrivalAirportId <= 0)
            throw new ValidationException("Arrival airport must be valid.");

        if (criteria.DepartureAirportId == criteria.ArrivalAirportId)
            throw new ValidationException("Departure and arrival airports cannot be the same.");

        if (criteria.DepartureDate.Date < DateTime.UtcNow.Date)
            throw new ValidationException("Departure date cannot be in the past.");

        if (criteria.PassengerCount <= 0)
            throw new ValidationException("Passenger count must be greater than 0.");

        _logger.LogInformation(
            "Searching flights: From {DepartureId} to {ArrivalId} on {DepartureDate} for {PassengerCount} passengers",
            criteria.DepartureAirportId,
            criteria.ArrivalAirportId,
            criteria.DepartureDate,
            criteria.PassengerCount);

        try
        {
            // 1. Generate cache key
            var cacheKey = string.Format(
                FLIGHTS_SEARCH_CACHE_KEY,
                criteria.DepartureAirportId,
                criteria.ArrivalAirportId,
                criteria.DepartureDate.Date.Ticks);

            // 2. Check cache
            var cachedResults = await _cacheService.GetAsync<List<FlightResponseDto>>(cacheKey);
            
            if (cachedResults != null)
            {
                _logger.LogInformation("Search results found in cache for key: {CacheKey}", cacheKey);
                // Filter by seat class if provided
                var filtered = FilterByAircraft(cachedResults, criteria.SeatClass).ToList();
                return filtered;
            }

            // 3. Query repository
            var flights = await _flightRepository.SearchAsync(
                criteria.DepartureAirportId,
                criteria.ArrivalAirportId,
                criteria.DepartureDate,
                criteria.PassengerCount);

            // 4. Map to DTOs
            var dtos = flights.Select(f => MapFlightToResponseDto(f)).ToList();

            // 5. Filter by seat class if provided
            if (!string.IsNullOrEmpty(criteria.SeatClass))
            {
                dtos = FilterByAircraft(dtos, criteria.SeatClass).ToList();
            }

            // 6. Cache results with 30-minute TTL
            await _cacheService.SetAsync(cacheKey, dtos, SEARCH_CACHE_TTL);

            // 7. Log result
            _logger.LogInformation(
                "Flight search completed: Found {Count} flights",
                dtos.Count);

            // 8. Return results
            return dtos;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching flights");
            throw;
        }
    }

    /// <summary>
    /// Creates a new flight.
    /// Algorithm:
    /// 1. Validate DTO
    /// 2. Map DTO to Flight entity
    /// 3. Validate entity constraints
    /// 4. Add to repository
    /// 5. Save changes
    /// 6. Invalidate related caches
    /// 7. Map back to ResponseDto
    /// 8. Log creation
    /// 9. Return ResponseDto
    /// </summary>
    public async Task<FlightResponseDto> CreateFlightAsync(FlightCreateDto dto)
    {
        if (dto == null)
            throw new ValidationException("Flight data cannot be null.");

        if (string.IsNullOrEmpty(dto.FlightNumber))
            throw new ValidationException("Flight number is required.");

        if (dto.DepartureAirportId <= 0)
            throw new ValidationException("Departure airport must be valid.");

        if (dto.ArrivalAirportId <= 0)
            throw new ValidationException("Arrival airport must be valid.");

        if (dto.DepartureAirportId == dto.ArrivalAirportId)
            throw new ValidationException("Departure and arrival airports cannot be the same.");

        if (dto.DepartureTime >= dto.ArrivalTime)
            throw new ValidationException("Departure time must be before arrival time.");

        if (dto.DepartureTime <= DateTime.UtcNow)
            throw new ValidationException("Departure time cannot be in the past.");

        if (dto.TotalSeats <= 0)
            throw new ValidationException("Total seats must be greater than 0.");

        if (dto.BasePrice < 0)
            throw new ValidationException("Base price cannot be negative.");

        _logger.LogInformation("Creating new flight: {FlightNumber}", dto.FlightNumber);

        try
        {
            // 1. Map DTO to entity
            var flight = new Flight
            {
                FlightNumber = dto.FlightNumber,
                DepartureAirportId = dto.DepartureAirportId,
                ArrivalAirportId = dto.ArrivalAirportId,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                TotalSeats = dto.TotalSeats,
                BasePrice = dto.BasePrice
            };
            
            // 2. Initialize properties not in DTO
            flight.AvailableSeats = dto.TotalSeats;
            flight.Status = Domain.Enums.FlightStatus.Active;
            flight.CreatedAt = DateTime.UtcNow;
            flight.UpdatedAt = DateTime.UtcNow;

            // 3. Add to repository
            var createdFlight = await _flightRepository.AddAsync(flight);

            // 4. Save changes
            await _flightRepository.SaveChangesAsync();

            // 5. Invalidate caches (search results may have changed)
            await _cacheService.RemoveByPatternAsync("flights_search_*");

            // 6. Map to DTO
            var responseDto = MapFlightToResponseDto(createdFlight);

            // 7. Log success
            _logger.LogInformation(
                "Successfully created flight {FlightId}: {FlightNumber}",
                createdFlight.Id,
                createdFlight.FlightNumber);

            // 8. Return DTO
            return responseDto;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flight");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing flight.
    /// Algorithm:
    /// 1. Fetch existing flight
    /// 2. If not found, throw exception
    /// 3. Validate update eligibility
    /// 4. Apply changes from update DTO
    /// 5. Validate updated entity
    /// 6. Update repository
    /// 7. Save changes
    /// 8. Invalidate cache for this flight
    /// 9. Invalidate search result caches
    /// 10. Map to ResponseDto
    /// 11. Log update
    /// 12. Return ResponseDto
    /// </summary>
    public async Task<FlightResponseDto> UpdateFlightAsync(int id, FlightUpdateDto dto)
    {
        if (id <= 0)
            throw new ValidationException("Flight ID must be greater than 0.");

        if (dto == null)
            throw new ValidationException("Update data cannot be null.");

        _logger.LogInformation("Updating flight with ID: {FlightId}", id);

        try
        {
            // 1. Fetch existing flight
            var flight = await _flightRepository.GetByIdAsync(id);

            // 2. Check if found
            if (flight == null)
            {
                _logger.LogWarning("Flight with ID {FlightId} not found for update", id);
                throw new FlightNotFoundException(id);
            }

            // 3. Apply changes (only non-null properties)
            if (!string.IsNullOrEmpty(dto.FlightNumber))
                flight.FlightNumber = dto.FlightNumber;

            if (dto.DepartureAirportId.HasValue)
                flight.DepartureAirportId = dto.DepartureAirportId.Value;

            if (dto.ArrivalAirportId.HasValue)
                flight.ArrivalAirportId = dto.ArrivalAirportId.Value;

            if (dto.DepartureTime.HasValue)
                flight.DepartureTime = dto.DepartureTime.Value;

            if (dto.ArrivalTime.HasValue)
                flight.ArrivalTime = dto.ArrivalTime.Value;

            if (!string.IsNullOrEmpty(dto.Airline))
                flight.Airline = dto.Airline;

            if (!string.IsNullOrEmpty(dto.AircraftModel))
                flight.AircraftModel = dto.AircraftModel;

            if (dto.TotalSeats.HasValue)
            {
                var newTotalSeats = dto.TotalSeats.Value;
                var reservedSeats = flight.TotalSeats - flight.AvailableSeats;
                
                if (newTotalSeats < reservedSeats)
                    throw new ValidationException(
                        $"Cannot reduce total seats below {reservedSeats} (already reserved).");
                
                flight.TotalSeats = newTotalSeats;
                flight.AvailableSeats = newTotalSeats - reservedSeats;
            }

            if (dto.BasePrice.HasValue)
            {
                if (dto.BasePrice.Value < 0)
                    throw new ValidationException("Base price cannot be negative.");
                
                flight.BasePrice = dto.BasePrice.Value;
            }

            // 4. Validate constraints
            if (flight.DepartureAirportId == flight.ArrivalAirportId)
                throw new ValidationException("Departure and arrival airports cannot be the same.");

            if (flight.DepartureTime >= flight.ArrivalTime)
                throw new ValidationException("Departure time must be before arrival time.");

            // 5. Update timestamp
            flight.UpdatedAt = DateTime.UtcNow;

            // 6. Update repository
            await _flightRepository.UpdateAsync(flight);

            // 7. Save changes
            await _flightRepository.SaveChangesAsync();

            // 8. Invalidate flight cache
            var flightCacheKey = string.Format(FLIGHT_CACHE_KEY, id);
            await _cacheService.RemoveAsync(flightCacheKey);

            // 9. Invalidate search caches
            await _cacheService.RemoveByPatternAsync("flights_search_*");

            // 10. Map to DTO
            var responseDto = MapFlightToResponseDto(flight);

            // 11. Log success
            _logger.LogInformation(
                "Successfully updated flight {FlightId}: {FlightNumber}",
                flight.Id,
                flight.FlightNumber);

            // 12. Return DTO
            return responseDto;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight with ID: {FlightId}", id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a flight.
    /// Algorithm:
    /// 1. Fetch flight
    /// 2. If not found, throw exception
    /// 3. Check if flight can be deleted
    /// 4. Delete from repository
    /// 5. Save changes
    /// 6. Invalidate cache
    /// 7. Log deletion
    /// </summary>
    public async Task DeleteFlightAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Flight ID must be greater than 0.");

        _logger.LogInformation("Deleting flight with ID: {FlightId}", id);

        try
        {
            // 1. Fetch flight
            var flight = await _flightRepository.GetByIdAsync(id);

            // 2. Check if found
            if (flight == null)
            {
                _logger.LogWarning("Flight with ID {FlightId} not found for deletion", id);
                throw new FlightNotFoundException(id);
            }

            // 3. Check if can be deleted (no active bookings)
            var reservedSeats = flight.TotalSeats - flight.AvailableSeats;
            if (reservedSeats > 0)
            {
                _logger.LogWarning(
                    "Cannot delete flight {FlightId} with {ReservedSeats} reserved seats",
                    id,
                    reservedSeats);
                throw new InvalidOperationException(
                    $"Cannot delete flight with {reservedSeats} active bookings.");
            }

            // 4. Delete from repository
            await _flightRepository.DeleteAsync(flight);

            // 5. Save changes
            await _flightRepository.SaveChangesAsync();

            // 6. Invalidate cache
            var flightCacheKey = string.Format(FLIGHT_CACHE_KEY, id);
            await _cacheService.RemoveAsync(flightCacheKey);
            await _cacheService.RemoveByPatternAsync("flights_search_*");

            // 7. Log success
            _logger.LogInformation("Successfully deleted flight {FlightId}", id);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (FlightNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flight with ID: {FlightId}", id);
            throw;
        }
    }

    /// <summary>
    /// Helper method to filter flights by aircraft/seat class.
    /// Note: This is a simplified implementation. In production, you would filter by actual seat classes
    /// stored in the database or aircraft configuration.
    /// </summary>
    private IEnumerable<FlightResponseDto> FilterByAircraft(IEnumerable<FlightResponseDto> flights, string? seatClass)
    {
        if (string.IsNullOrEmpty(seatClass))
            return flights;

        // This is a placeholder filter. In production, filter based on actual seat class data
        // For now, all flights returned unless seat class is very specific
        return flights.Where(f => f.AircraftModel != null);
    }

    /// <summary>
    /// Maps a Flight entity to FlightResponseDto.
    /// </summary>
    private FlightResponseDto MapFlightToResponseDto(Flight flight)
    {
        return new FlightResponseDto
        {
            Id = flight.Id,
            FlightNumber = flight.FlightNumber,
            DepartureAirportId = flight.DepartureAirportId,
            DepartureAirportName = flight.DepartureAirport?.Name ?? string.Empty,
            DepartureAirportCode = flight.DepartureAirport?.Code ?? string.Empty,
            ArrivalAirportId = flight.ArrivalAirportId,
            ArrivalAirportName = flight.ArrivalAirport?.Name ?? string.Empty,
            ArrivalAirportCode = flight.ArrivalAirport?.Code ?? string.Empty,
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            Airline = flight.Airline ?? string.Empty,
            AircraftModel = flight.AircraftModel ?? string.Empty,
            TotalSeats = flight.TotalSeats,
            AvailableSeats = flight.AvailableSeats,
            BasePrice = flight.BasePrice,
            Status = flight.Status.ToString(),
            CreatedAt = flight.CreatedAt,
            UpdatedAt = flight.UpdatedAt
        };
    }
}
