using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class SearchFlightService(
    IFlightRepository flightRepository,
    IFlightInventoryRepository inventoryRepository,
    IPricingService pricingService) : ISearchFlightService
{
    public async Task<IEnumerable<FlightSearchResultDto>> SearchAsync(FlightSearchRequest request)
    {
        var flights = await flightRepository.SearchAsync(
            request.OriginCode,
            request.DestinationCode,
            request.DepartureDate,
            request.FareClassCode);

        var results = new List<FlightSearchResultDto>();
        int totalPassengers = request.AdultCount + request.ChildCount;

        foreach (var flight in flights)
        {
            var inventories = await inventoryRepository.GetByFlightAsync(flight.Id);
            var fareOptions = new List<FareOptionDto>();

            foreach (var inv in inventories)
            {
                if (request.FareClassCode is not null && inv.FareClass.Code != request.FareClassCode)
                    continue;

                if (inv.AvailableSeats < totalPassengers)
                    continue;

                var breakdown = await pricingService.CalculatePriceAsync(flight.Id, inv.FareClassId, totalPassengers);

                fareOptions.Add(new FareOptionDto(
                    inv.FareClassId,
                    inv.FareClass.Code,
                    inv.FareClass.Name,
                    inv.AvailableSeats,
                    breakdown.TotalPerPerson,
                    breakdown.TotalPerPerson * totalPassengers,
                    inv.FareClass.IsRefundable,
                    inv.FareClass.CheckedBaggageKg));
            }

            if (fareOptions.Count > 0)
                results.Add(new FlightSearchResultDto(MapFlightToDto(flight), fareOptions));
        }

        return results.OrderBy(r => r.FareOptions.Min(fo => fo.PricePerPerson));
    }

    public async Task<IEnumerable<FlightSearchResultDto>> SearchRoundTripAsync(RoundTripSearchRequest request)
    {
        var outbound = await SearchAsync(new FlightSearchRequest(
            request.OriginCode, request.DestinationCode, request.DepartureDate,
            request.AdultCount, request.ChildCount, request.InfantCount));

        var inbound = await SearchAsync(new FlightSearchRequest(
            request.DestinationCode, request.OriginCode, request.ReturnDate,
            request.AdultCount, request.ChildCount, request.InfantCount));

        // Return both legs; the API layer combines them
        return outbound.Concat(inbound);
    }

    private static FlightDto MapFlightToDto(Flight f) => new(
        f.Id, f.FlightNumber,
        new RouteDto(f.Route.Id, f.Route.OriginAirport.Code, f.Route.OriginAirport.City,
                     f.Route.DestinationAirport.Code, f.Route.DestinationAirport.City,
                     f.Route.IsDomestic, f.Route.IsActive),
        new AircraftDto(f.Aircraft.Id, f.Aircraft.Code, f.Aircraft.Model, f.Aircraft.Manufacturer, f.Aircraft.TotalSeats, f.Aircraft.IsActive),
        f.DepartureTime, f.ArrivalTime, f.Status, f.GateNumber,
        [],
        []);
}
