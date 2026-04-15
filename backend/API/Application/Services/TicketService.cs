namespace API.Application.Services;

using API.Application.Dtos.Ticket;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingPassengerRepository _passengerRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository,
        IBookingPassengerRepository passengerRepository,
        IFlightRepository flightRepository,
        IEmailService emailService,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _bookingRepository = bookingRepository;
        _passengerRepository = passengerRepository;
        _flightRepository = flightRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<List<TicketResponse>> CreateTicketsAsync(int bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            var flight = await _flightRepository.GetByIdAsync(booking.OutboundFlightId);
            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
            var tickets = new List<TicketResponse>();

            int sequenceNumber = 1;
            foreach (var passenger in passengers)
            {
                var ticketNumber = GenerateTicketNumber(booking.BookingCode, sequenceNumber++);
                var ticket = new Ticket
                {
                    BookingPassengerId = passenger.Id,
                    TicketNumber = ticketNumber,
                    Status = 0, // Issued
                    IssuedAt = DateTime.UtcNow
                };

                var createdTicket = await _ticketRepository.CreateAsync(ticket);
                tickets.Add(new TicketResponse
                {
                    TicketId = createdTicket.Id,
                    TicketNumber = ticketNumber,
                    BookingId = bookingId,
                    PassengerId = passenger.Id,
                    PassengerName = passenger.FullName,
                    FlightId = flight!.Id,
                    FlightNumber = flight.FlightNumber,
                    SeatNumber = "TBD",
                    Status = "Issued",
                    IssuedAt = createdTicket.IssuedAt,
                    DepartureTime = flight.DepartureTime,
                    DepartureAirport = flight.Route.DepartureAirport.Code,
                    ArrivalAirport = flight.Route.ArrivalAirport.Code
                });
            }

            _logger.LogInformation("Tickets created for booking {BookingId}: {Count} tickets", bookingId, tickets.Count);
            return tickets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tickets");
            throw;
        }
    }

    public async Task<TicketResponse> GetTicketAsync(string ticketNumber)
    {
        try
        {
            var ticket = await _ticketRepository.GetByTicketNumberAsync(ticketNumber);
            if (ticket == null)
            {
                throw new NotFoundException("Ticket not found");
            }

            var passenger = await _passengerRepository.GetByIdAsync(ticket.BookingPassengerId);
            var booking = await _bookingRepository.GetByIdAsync(passenger!.BookingId);
            var flight = await _flightRepository.GetByIdAsync(booking!.OutboundFlightId);

            var statusString = ticket.Status switch
            {
                0 => "Issued",
                1 => "Used",
                2 => "Refunded",
                3 => "Cancelled",
                _ => "Unknown"
            };

            return new TicketResponse
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                BookingId = booking.Id,
                PassengerId = passenger.Id,
                PassengerName = passenger.FullName,
                FlightId = flight!.Id,
                FlightNumber = flight.FlightNumber,
                SeatNumber = "TBD",
                Status = statusString,
                IssuedAt = ticket.IssuedAt,
                DepartureTime = flight.DepartureTime,
                DepartureAirport = flight.Route.DepartureAirport.Code,
                ArrivalAirport = flight.Route.ArrivalAirport.Code
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket");
            throw;
        }
    }

    public async Task<bool> ChangeTicketAsync(string ticketNumber, ChangeTicketDto dto)
    {
        try
        {
            var ticket = await _ticketRepository.GetByTicketNumberAsync(ticketNumber);
            if (ticket == null)
            {
                throw new NotFoundException("Ticket not found");
            }

            var flight = await _flightRepository.GetByIdAsync(dto.NewFlightId);
            if (flight == null || flight.DepartureTime < DateTime.UtcNow)
            {
                throw new ValidationException("Invalid flight for ticket change");
            }

            // Update ticket
            ticket.ReplacedByTicketId = null; // Would create new ticket in production
            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation("Ticket changed: {TicketNumber}", ticketNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing ticket");
            throw;
        }
    }

    public async Task<List<TicketResponse>> GetBookingTicketsAsync(int bookingId)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException("Booking not found");
            }

            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
            var tickets = new List<TicketResponse>();

            foreach (var passenger in passengers)
            {
                var ticket = await _ticketRepository.GetByPassengerIdAsync(passenger.Id);
                if (ticket != null)
                {
                    var flight = await _flightRepository.GetByIdAsync(booking.OutboundFlightId);
                    var statusString = ticket.Status switch
                    {
                        0 => "Issued",
                        1 => "Used",
                        2 => "Refunded",
                        3 => "Cancelled",
                        _ => "Unknown"
                    };

                    tickets.Add(new TicketResponse
                    {
                        TicketId = ticket.Id,
                        TicketNumber = ticket.TicketNumber,
                        BookingId = bookingId,
                        PassengerId = passenger.Id,
                        PassengerName = passenger.FullName,
                        FlightId = flight!.Id,
                        FlightNumber = flight.FlightNumber,
                        SeatNumber = "TBD",
                        Status = statusString,
                        IssuedAt = ticket.IssuedAt,
                        DepartureTime = flight.DepartureTime,
                        DepartureAirport = flight.Route.DepartureAirport.Code,
                        ArrivalAirport = flight.Route.ArrivalAirport.Code
                    });
                }
            }

            return tickets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking tickets");
            throw;
        }
    }

    public async Task<byte[]> DownloadTicketAsync(string ticketNumber, string format = "pdf")
    {
        try
        {
            var ticket = await GetTicketAsync(ticketNumber);
            var html = GenerateHtmlTicket(ticket);
            return System.Text.Encoding.UTF8.GetBytes(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading ticket");
            throw;
        }
    }

    private string GenerateTicketNumber(string bookingCode, int passengerSequence)
    {
        return $"FL-{bookingCode}-{passengerSequence:D3}";
    }

    private string GenerateHtmlTicket(TicketResponse ticket)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Flight Ticket</h1>
                <p><strong>Ticket:</strong> {ticket.TicketNumber}</p>
                <p><strong>Passenger:</strong> {ticket.PassengerName}</p>
                <p><strong>Flight:</strong> {ticket.FlightNumber}</p>
                <p><strong>Departure:</strong> {ticket.DepartureTime:yyyy-MM-dd HH:mm}</p>
                <p><strong>Route:</strong> {ticket.DepartureAirport} → {ticket.ArrivalAirport}</p>
            </body>
            </html>";
    }
}
