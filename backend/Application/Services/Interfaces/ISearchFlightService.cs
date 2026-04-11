using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface ISearchFlightService
{
    Task<IEnumerable<FlightSearchResultDto>> SearchAsync(FlightSearchRequest request);
    Task<IEnumerable<FlightSearchResultDto>> SearchRoundTripAsync(RoundTripSearchRequest request);
}
