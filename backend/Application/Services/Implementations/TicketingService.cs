using FlightBooking.Application.DTOs.Booking;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class TicketingService(ITicketRepository ticketRepository, IBookingRepository bookingRepository) : ITicketingService
{
    public async Task<TicketDto> GetByNumberAsync(string ticketNumber)
    {
        var ticket = await ticketRepository.GetByNumberAsync(ticketNumber)
            ?? throw new KeyNotFoundException($"Ticket {ticketNumber} not found.");
        return MapToDto(ticket);
    }

    public async Task<IEnumerable<TicketDto>> GetByBookingAsync(Guid bookingId)
    {
        var tickets = await ticketRepository.GetByBookingAsync(bookingId);
        return tickets.Select(MapToDto);
    }

    public async Task<IEnumerable<TicketDto>> IssueTicketsAsync(Guid bookingId)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status != "confirmed")
            throw new InvalidOperationException("Cannot issue tickets for a non-confirmed booking.");

        var tickets = new List<Ticket>();
        int sequence = 1;

        foreach (var item in booking.Items.Where(i => i.Status == "active"))
        {
            if (item.Ticket is not null) continue; // Already issued

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                TicketNumber = GenerateTicketNumber(sequence++),
                BookingItemId = item.Id,
                Status = "issued",
                IssuedAt = DateTime.UtcNow
            };
            tickets.Add(ticket);
        }

        await ticketRepository.AddRangeAsync(tickets);
        await ticketRepository.SaveChangesAsync();

        return tickets.Select(MapToDto);
    }

    public async Task CancelTicketAsync(Guid ticketId, string reason)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        ticket.Status = "cancelled";
        await ticketRepository.SaveChangesAsync();
    }

    public async Task UseTicketAsync(Guid ticketId)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        if (ticket.Status != "issued")
            throw new InvalidOperationException($"Ticket is in status '{ticket.Status}' and cannot be used.");

        ticket.Status = "used";
        await ticketRepository.SaveChangesAsync();
    }

    private static string GenerateTicketNumber(int sequence)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        return $"TK-{datePart}-{sequence:D5}";
    }

    private static TicketDto MapToDto(Ticket t) =>
        new(t.Id, t.TicketNumber, t.Status, t.IssuedAt, t.BoardingPassUrl, t.ETicketUrl);
}
