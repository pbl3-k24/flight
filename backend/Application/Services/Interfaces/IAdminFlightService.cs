using FlightBooking.Application.DTOs.Admin;
using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IAdminFlightService
{
    Task<AdminFlightSummaryDto> GetFlightSummaryAsync(Guid flightId);
    Task<IEnumerable<AdminFlightSummaryDto>> GetFlightsAsync(AdminFlightFilter filter);
    Task BulkCancelFlightsAsync(IEnumerable<Guid> flightIds, string reason, Guid adminId);
    Task AssignAircraftAsync(Guid flightId, Guid aircraftId, Guid adminId);
}
