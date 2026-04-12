namespace API.Application.Interfaces;

using API.Application.DTOs;

/// <summary>
/// Service interface for flight-related operations.
/// Defines contracts for flight business logic orchestration.
/// All methods are asynchronous and return Tasks for proper async/await patterns.
/// </summary>
public interface IFlightService
{
    /// <summary>
    /// Gets a specific flight by ID.
    /// Retrieves flight details including current seat availability and status.
    /// </summary>
    /// <param name="id">The flight ID to retrieve.</param>
    /// <returns>Flight details if found.</returns>
    /// <exception cref="FlightNotFoundException">Thrown when flight is not found.</exception>
    Task<FlightResponseDto> GetFlightAsync(int id);

    /// <summary>
    /// Searches for flights matching specified criteria.
    /// Filters flights by departure airport, arrival airport, date, and seat class.
    /// Validates seat availability for the requested passenger count.
    /// </summary>
    /// <param name="criteria">Search criteria including departure/arrival airports, date, and passenger count.</param>
    /// <returns>Collection of matching flights sorted by departure time.</returns>
    /// <exception cref="ValidationException">Thrown when search criteria is invalid.</exception>
    Task<IEnumerable<FlightResponseDto>> SearchFlightsAsync(FlightSearchDto criteria);

    /// <summary>
    /// Creates a new flight in the system.
    /// Validates airport existence, time constraints, and other business rules.
    /// Initializes available seats equal to total seats.
    /// </summary>
    /// <param name="dto">Flight creation data.</param>
    /// <returns>Newly created flight details.</returns>
    /// <exception cref="FlightNotFoundException">Thrown when referenced airport is not found.</exception>
    /// <exception cref="ValidationException">Thrown when flight data is invalid or violates business rules.</exception>
    Task<FlightResponseDto> CreateFlightAsync(FlightCreateDto dto);

    /// <summary>
    /// Updates an existing flight with new information.
    /// Only updatable fields can be modified. Status and ID are not changeable.
    /// Validates business rules before applying changes.
    /// </summary>
    /// <param name="id">The flight ID to update.</param>
    /// <param name="dto">Updated flight data (nullable properties mean "no change").</param>
    /// <returns>Updated flight details.</returns>
    /// <exception cref="FlightNotFoundException">Thrown when flight is not found.</exception>
    /// <exception cref="ValidationException">Thrown when updated data is invalid or violates business rules.</exception>
    Task<FlightResponseDto> UpdateFlightAsync(int id, FlightUpdateDto dto);

    /// <summary>
    /// Deletes a flight from the system.
    /// Validates that flight can be safely deleted (no active bookings).
    /// </summary>
    /// <param name="id">The flight ID to delete.</param>
    /// <returns>Completed task.</returns>
    /// <exception cref="FlightNotFoundException">Thrown when flight is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when flight has active bookings and cannot be deleted.</exception>
    Task DeleteFlightAsync(int id);
}
