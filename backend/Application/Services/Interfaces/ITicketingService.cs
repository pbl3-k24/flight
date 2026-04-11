using FlightBooking.Application.DTOs.Booking;

namespace FlightBooking.Application.Services.Interfaces;

public interface ITicketingService
{
    Task<TicketDto> GetByNumberAsync(string ticketNumber);
    Task<IEnumerable<TicketDto>> GetByBookingAsync(Guid bookingId);

    /// <summary>Issue tickets for all booking items after payment confirmation.</summary>
    Task<IEnumerable<TicketDto>> IssueTicketsAsync(Guid bookingId);

    /// <summary>Cancel a specific ticket (for partial refund scenarios).</summary>
    Task CancelTicketAsync(Guid ticketId, string reason);

    /// <summary>Mark ticket as used (check-in).</summary>
    Task UseTicketAsync(Guid ticketId);
}
