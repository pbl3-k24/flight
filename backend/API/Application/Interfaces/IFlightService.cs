namespace API.Application.Interfaces;

using API.Application.Dtos.Flight;

public interface IFlightService
{
    /// <summary>
    /// Searches for available flights based on criteria.
    /// </summary>
    /// <param name="criteria">Flight search criteria</param>
    /// <returns>List of available flights</returns>
    Task<List<FlightSearchResponse>> SearchAsync(FlightSearchDto criteria);

    /// <summary>
    /// Gets detailed information about a specific flight.
    /// </summary>
    /// <param name="flightId">Flight ID</param>
    /// <returns>Flight details with seat inventory</returns>
    Task<FlightDetailResponse> GetFlightAsync(int flightId);

    /// <summary>
    /// Gets the number of available seats for a flight and seat class.
    /// </summary>
    /// <param name="flightId">Flight ID</param>
    /// <param name="seatClassId">Seat class ID</param>
    /// <returns>Available seat count</returns>
    Task<int> GetAvailableSeatsAsync(int flightId, int seatClassId);

    /// <summary>
    /// Gets all flights for a specific route.
    /// </summary>
    /// <param name="routeId">Route ID</param>
    /// <param name="departureDate">Departure date filter</param>
    /// <returns>List of flights for the route</returns>
    Task<List<FlightSearchResponse>> GetFlightsByRouteAsync(int routeId, DateTime departureDate);
}
