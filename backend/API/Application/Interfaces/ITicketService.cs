namespace API.Application.Interfaces;

using API.Application.Dtos.Ticket;

public interface ITicketService
{
    /// <summary>
    /// Creates tickets for all passengers in a booking.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>List of created tickets</returns>
    Task<List<TicketResponse>> CreateTicketsAsync(int bookingId);

    /// <summary>
    /// Gets a ticket by ticket number.
    /// </summary>
    /// <param name="ticketNumber">Ticket number</param>
    /// <returns>Ticket details</returns>
    Task<TicketResponse> GetTicketAsync(string ticketNumber);

    /// <summary>
    /// Changes a ticket to a different flight.
    /// </summary>
    /// <param name="ticketNumber">Ticket number</param>
    /// <param name="dto">Change details</param>
    /// <returns>Success indicator</returns>
    Task<bool> ChangeTicketAsync(string ticketNumber, ChangeTicketDto dto);

    /// <summary>
    /// Gets all tickets for a booking.
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>List of booking tickets</returns>
    Task<List<TicketResponse>> GetBookingTicketsAsync(int bookingId);

    /// <summary>
    /// Downloads ticket as PDF or HTML.
    /// </summary>
    /// <param name="ticketNumber">Ticket number</param>
    /// <param name="format">pdf or html</param>
    /// <returns>File bytes</returns>
    Task<byte[]> DownloadTicketAsync(string ticketNumber, string format = "pdf");
}
